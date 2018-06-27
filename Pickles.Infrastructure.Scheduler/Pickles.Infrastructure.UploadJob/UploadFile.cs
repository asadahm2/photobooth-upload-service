using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pickles.Infrastructure.Common;
using Pickles.Infrastructure.Data;
using Pickles.Infrastructure.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace Pickles.Infrastructure.UploadJob
{

	public class UploadFile : IJob
	{
		const string DATA_TABLE_NAME = "uploadfilejob";

		private readonly ILogger Logger;
		private readonly IData Data;
		//private ConcurrentQueue<UploadInfo> UploadFileQueue;
		private readonly UploadFileConfiguration JobConfiguration;
		private int processedPhotos = 0;
		private int totalPhotos = 0;
		private string loadNumber = String.Empty;
		private string[] loadFiles = new string[] { };
		private string[] processFiles = new string[] { };
		private string[] processedFiles = new string[] { };
		private string[] completeFile = new string[] { };
		private Dictionary<string, int> fileOrder = new Dictionary<string, int>();

		public UploadFile(ILogger logger, IData data)
		{
			Logger = logger;
			Data = data;
			JobConfiguration = ConfigurationManager.GetSection("uploadFileSettings") as UploadFileConfiguration;
		}

		public void Execute(IJobExecutionContext context)
		{
			try
			{
				List<UploadSet> fileSetsToUpload = new List<UploadSet>();

				List<UploadInfo> filesToUpload = new List<UploadInfo>();

				Logger.LogCustomEvent("UploadFile.Execute");

				#region Find the files to upload

				//Implement Idempotency - this job can be run multiple times without any negative effect.
				//Files already uploaded will not be uploaded again even if the job executes multiple times.

				foreach (SetupConfigurationElement item in JobConfiguration.Setups)
				{
					filesToUpload.Clear();

					#region Find files for each setup

					//get all files from the folder

					AddSeqToFiles(item.UserName, item.Password, item.FilePath, "*." + item.FileExtension);
										
					var filesInDirToUpload = GetFiles(item.ImagesPerCall).ToList();

					//at this point we will either have new images or previously sucessfully uploaded images
					//we need to include all the new ones and any previously successfully uploaded image that has been retaken
					foreach (var file in filesInDirToUpload)
					{
						#region Add Files to Upload

						FileInfo fi = new FileInfo(file);

						UploadInfo ui = new UploadInfo
						{
							Id = Guid.NewGuid().ToString(),
							FileName = file,
							UploadEndPoint = item.UploadEndpoint,
							Status = UploadSet.UploadStatus.NotStarted,
							SourceMachineName = item.MachineName,
							FileCreationTimeUtc = fi.LastWriteTimeUtc,
							//mailEndPoint = item.UploadEndpoint,
						};

						filesToUpload.Add(ui);

						#endregion
					}

					#endregion

					//make sure the files to upload belong to a full set
					var loadSets = filesToUpload.GroupBy(f => f.FileName.ToLoadNumber());

					foreach (var set in loadSets)
					{
						fileSetsToUpload.Add(new UploadSet { LoadNumber = set.Key, Setup = item, UploadInfoList = set.ToList(), IsRetake = set.First().IsRetake });
					}
				}

				#endregion


				//exclude any files that are currently being uploaded
				foreach (var file in UploadFileQueue.Instance.InProgress)
				{
					var existing = fileSetsToUpload.Where(f => f.LoadNumber.Equals(file, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

					if (existing != null)
					{
						fileSetsToUpload.Remove(existing);
					}
				}

				//queue the file uploads
				foreach (var item in fileSetsToUpload)
				{
					Logger.LogCustomEvent("QueueUpload", item.ToProperties());

					UploadFileQueue.Instance.Queue.Enqueue(item);
				}

				if (fileSetsToUpload.Count == 0)
				{
					Logger.LogCustomEvent("NoFilesToUpload");

					Console.WriteLine("No file sets to upload: " + context.JobDetail.Description);
				}

				do
				{
					List<UploadSet> filesToProcess = new List<UploadSet>();

					for (int i = 0; i < JobConfiguration.NumberOfThreads; i++)
					{
						UploadSet res = null;

						if (UploadFileQueue.Instance.Queue.TryDequeue(out res))
						{
							filesToProcess.Add(res);
						}
					}

					if (filesToProcess.Count() == 0)
					{
						break;
					}

					List<Task> tasks = new List<Task>();
					try
					{
						foreach (var file in filesToProcess)
						{
							tasks.Add(Task.Factory.StartNew(() =>
							{

								if (loadNumber.Length > 0 && loadFiles.Count() == 0)
								{
									Console.WriteLine("No complete file for the load: " + loadNumber + " " + context.JobDetail.Description);
									Console.WriteLine("move all photos for load " + loadNumber + " to error folder");
									Logger.LogException(new Exception("No complete file for the load: " + loadNumber + " " + context.JobDetail.Description));
									MoveFiles(file, loadFiles, true);

								}
								else if (loadNumber.Length > 0 && loadFiles.Count() > 0 && loadFiles.Count() != totalPhotos)
								{
									Console.WriteLine("file number on complete file is different from the total number of photos for load: " + loadNumber + " " + context.JobDetail.Description);
									Console.WriteLine("move all photos for load " + loadNumber + " to error folder");
									Logger.LogException(new Exception("file number on complete file is different from the total number of photos for load: " + loadNumber + " " + context.JobDetail.Description));
									MoveFiles(file, loadFiles, true);
									MoveFiles(file, completeFile, true);

								}
								else
								{
									UploadFileToServer(file);
								}
							}));
						}
						Task.WaitAll(tasks.ToArray());
					}
					catch (AggregateException excep)
					{
						foreach (var task in tasks)
						{
							if (task.Exception != null)
							{
								UploadInfo chunk = task.AsyncState as UploadInfo;
								chunk.Status = UploadSet.UploadStatus.Failed;
							}
						}
					}
				}
				while (true);
			}
			catch (Exception ex)
			{
				//log exception
				Logger.LogException(ex);
			}

		}


		private void UploadFileToServer(UploadSet uploadSet)
		{
			var startTimeUtc = DateTime.UtcNow;
			string deActivationErrorMessage = String.Empty;

			try
			{
				UploadFileQueue.Instance.InProgress.Add(uploadSet.LoadNumber);

				Logger.LogCustomEvent("ProcessUpload", uploadSet.ToProperties());

				Console.WriteLine("Uploading load: " + uploadSet.LoadNumber + " " + DateTime.Now.ToString());

				//get the item id
				string itemId = GetItemId(uploadSet.LoadNumber); //TODO In case of PAS web service connectivity issue, don't move the files

				#region validate

				if (String.IsNullOrEmpty(JobConfiguration.UploadSource))
				{
					throw new Exception("UploadSource not found in Configuration for load: " + uploadSet.LoadNumber);
				}

				if (uploadSet.Setup == null)
				{
					throw new Exception("Configuration setup not found for load: " + uploadSet.LoadNumber);
				}

				if (String.IsNullOrEmpty(uploadSet.Setup.UploadEndpoint))
				{
					throw new Exception("UploadEndpoint not found in Configuration setup for load: " + uploadSet.LoadNumber);
				}

				if (String.IsNullOrEmpty(uploadSet.Setup.SubscriptionId))
				{
					throw new Exception("SubscriptionId not found in Configuration setup for load: " + uploadSet.LoadNumber);
				}

				if (!FilesExist(uploadSet.Setup.UserName, uploadSet.Setup.Password, uploadSet.Setup.FilePath, uploadSet.UploadInfoList.Select(u => u.FileName).ToList()))
				{
					throw new NonLogException("Files not found.");
				}

				if (String.IsNullOrEmpty(itemId))
				{
					List<string> filesToMove = new List<string>();

					//take off _processed and add complete file
					for (int i = 0; i < loadFiles.Count(); i++)
					{
						if (loadFiles[i].Contains("_processed"))
						{
							filesToMove.Add(loadFiles[i].Substring(0, loadFiles[i].IndexOf("_proessed")) + ".jpg");
						}
						else
						{
							filesToMove.Add(loadFiles[i]);
						}
					}

					//add ready file
					filesToMove.Add(completeFile[0]);

					//Move to error folder
					MoveFiles(uploadSet, filesToMove.ToArray(), true);
					

					throw new NonLogException("Item Id not found for load: " + uploadSet.LoadNumber);
				}

				#endregion

				//Do the actual upload
				using (var httpClient = new HttpClient())
				{
					httpClient.Timeout = TimeSpan.FromMinutes(uploadSet.Setup.UploadTimeoutMinutes);
					httpClient.BaseAddress = new Uri(uploadSet.Setup.UploadEndpoint);

					httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", uploadSet.Setup.SubscriptionId);
					httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");

					List<string> itemPictureIds = new List<string>();

					#region Get item pictures id for retakes

					Logger.LogCustomEvent("GetItemId", uploadSet.ToProperties());

					Console.WriteLine("Calling Get endpoint:" + uploadSet.Setup.GetEndpoint + " at:" + DateTime.Now.ToString());

					var response = httpClient.GetAsync(uploadSet.Setup.GetEndpoint + "?item_id=" + itemId + "&stage=sale&active=true").Result;

					if (response.StatusCode != HttpStatusCode.OK)
					{
						var reason = response.ReasonPhrase;
						var errorMessage = response.Content.ReadAsStringAsync().Result;

						string error = "Upload for load: " + uploadSet.LoadNumber + " failed to get Item Picture Ids for retaken images. Reason: " + reason + ". Error: " + errorMessage +
									   " endpoint=" + uploadSet.Setup.GetEndpoint + " item_id=" + itemId;
						string subject = "Photo Booth Loading Error - Severity 2 - failed retrieve current photos";
						//SendEmail(uploadSet, subject, error);

						throw new Exception(error);

					}

					#endregion

					Dictionary<string, string> svcMetadata = new Dictionary<string, string>();
					Boolean saleStageOnly = true;

					#region Deactivate images

					if (processedPhotos == 0)
					{
						using (var content = new MultipartFormDataContent())
						{
							Console.WriteLine("Deactivating images");

							svcMetadata.Clear();

							var jsonResponse = response.Content.ReadAsStringAsync().Result;
							//get item picture ids from Json
							dynamic itemInfo = JObject.Parse(@jsonResponse);

							foreach (var pair in itemInfo)
							{
								
								var metadata = pair.First.metadata.ToString();
								var metadata_json = JObject.Parse(metadata);
								var stages = metadata_json.stage;
								var imageType = metadata_json.image_type;

								string stagesString = stages.ToString();
								string newStages = "";


								if (stagesString.Contains("["))
								{
									if (imageType == "photo")
									{
										saleStageOnly = false;
										newStages = stagesString.Replace("sale", "");
									}
								}
								else
								{
									saleStageOnly = true;
								}


								if (saleStageOnly)
								{
									string id = pair.Path;
									svcMetadata.Add(id, @"""" + id + @""":{""metadata"":{""active"":""false""}}");
								}
								else
								{
									string id = pair.Path;
									svcMetadata.Add(id, @"""" + id + @""":{""metadata"":{""stage"":" + newStages + "}}");
								}


							}

							if (svcMetadata.Count() > 0)
							{
								var jsonData = "{" + String.Join(",", svcMetadata.Values) + "}";

								content.Add(new StringContent("update"), "action");
								content.Add(new StringContent(CurrentEnvironment.UserName), "transactionUser");
								content.Add(new StringContent(jsonData), "images");

								Logger.LogCustomEvent("DeactiviatingImages", uploadSet.ToProperties());
								Console.WriteLine("Calling deactive upload endpoint:" + uploadSet.Setup.UploadEndpoint + " at:" + DateTime.Now.ToString());

								response = httpClient.PostAsync(uploadSet.Setup.UploadEndpoint, content).Result;

								if (response.StatusCode != HttpStatusCode.OK)
								{
									var reason = response.ReasonPhrase;
									deActivationErrorMessage = response.Content.ReadAsStringAsync().Result;
								}
							}
							
						}

					}


					#endregion

					using (var content = new MultipartFormDataContent())
					{
						#region Generate metadata

						string uploadFileName = String.Empty;

						svcMetadata.Clear();

						foreach (var item in uploadSet.UploadInfoList)
						{
							int sequenceNumber;
							fileOrder.TryGetValue(item.FileName.ToLower(), out sequenceNumber);

							var imgData = new
							{
								item_id = itemId,
								sequence_number = sequenceNumber.ToString(),
								visibility = "public",
								sync = "true",
								usage = GetUsage(item.FileName)
							};

							var s = new JsonSerializer();
							var sb = new StringBuilder();

							using (var w = new StringWriter(sb))
							{
								s.Serialize(w, imgData);
							}

							uploadFileName = item.FileName.ToFileName().Replace(uploadSet.LoadNumber, itemId);

							svcMetadata.Add(uploadFileName, @"""" + uploadFileName + @""":" + sb.ToString());
						}

						#endregion

						string imagesJson = @"{" + String.Join(",", svcMetadata.Values) + "}";

						content.Add(new StringContent(JobConfiguration.UploadSource), "source");

						content.Add(new StringContent("upload"), "action");
						content.Add(new StringContent(CurrentEnvironment.UserName), "transactionUser");
						content.Add(new StringContent(imagesJson), "images");

						Console.WriteLine(imagesJson);

						//get image data
						var imageData = GetImageData(uploadSet.Setup.UserName, uploadSet.Setup.Password, uploadSet.Setup.FilePath, uploadSet.UploadInfoList);

						//mime type should be the same for all image
						string mimeType = MimeMapping.GetMimeMapping(uploadSet.UploadInfoList.First().FileName);

						for (int i = 0; i < imageData.Count; i++)
						{
							uploadFileName = uploadSet.UploadInfoList[i].FileName;
							uploadFileName = uploadFileName.ToFileName().Replace(uploadSet.LoadNumber, itemId);

							var streamContent = new ByteArrayContent(imageData[i]);

							streamContent.Headers.Add("Content-Type", mimeType);

							streamContent.Headers.Add("Content-Disposition", "form-data; name=\"file\"; filename=\"" + uploadFileName + "\"");

							content.Add(streamContent, "files", uploadFileName);
						}

						Logger.LogCustomEvent("PostingImages", uploadSet.ToProperties());
						Console.WriteLine("Calling upload endpoint:" + uploadSet.Setup.UploadEndpoint + " at:" + DateTime.Now.ToString());

						response = httpClient.PostAsync(uploadSet.Setup.UploadEndpoint, content).Result;


						if (response.StatusCode != HttpStatusCode.OK)
						{
							var reason = response.ReasonPhrase;
							var errorMessage = response.Content.ReadAsStringAsync().Result;

							string error = "Upload for load: " + uploadSet.LoadNumber + " failed. Reason: " + reason + ". Error: " + errorMessage +
										   " endpoint=" + uploadSet.Setup.UploadEndpoint;
							string subject = "Photo Booth Loading Error - Severity 2 - failed upload photos";
							//SendEmail(uploadSet, subject, error);

							throw new Exception(error);
						}


					}


				}


				//Update status once uploaded
				foreach (var item in uploadSet.UploadInfoList)
				{
					item.Status = UploadSet.UploadStatus.Successful;
				}

				Console.WriteLine("Upload " + uploadSet.LoadNumber + " successful " + DateTime.Now.ToString());

				//add processed into file names.
				if (processedPhotos + uploadSet.UploadInfoList.Count < totalPhotos)
				{
					for (int i = 0; i < processFiles.Count(); i++)
					{
						System.IO.File.Move(processFiles[i],
											processFiles[i].Substring(0, processFiles[i].IndexOf(".jpg")) + "_processed" + ".jpg");
					}
				}
				else
				{
					//rename photo files -taking off processed flag.
					for (int i = 0; i < processedFiles.Count(); i++)
					{
						System.IO.File.Move(processedFiles[i],
											processedFiles[i].Substring(0, processedFiles[i].IndexOf("_processed")) + ".jpg");
					}

					var filesToMove = new List<string>();

					//take off _processed and add complete file
					for (int i = 0; i < loadFiles.Count(); i++)
					{
						if (loadFiles[i].Contains("_processed"))
						{
							filesToMove.Add(loadFiles[i].Substring(0, loadFiles[i].IndexOf("_processed")) + ".jpg");
						}
						else
						{
							filesToMove.Add(loadFiles[i]);
						}
					}

					//add ready file
					filesToMove.Add(filesToMove[0].Substring(0, filesToMove[0].IndexOf(loadNumber)) + loadNumber + "_complete_" + totalPhotos + ".ready");

					MoveFiles(uploadSet, filesToMove.ToArray());
				}

			}
			catch (Exception ex)
			{
				foreach (var item in uploadSet.UploadInfoList)
				{
					//don't mark upload status failed if there is an error in archiving the files
					if (ex.GetType() != typeof(FileArchiveException))
					{
						item.Status = UploadSet.UploadStatus.Failed;
					}

					item.ErrorMessage = ex.ToString();

					//log exception
					Logger.LogException(ex, item.ToProperties());
				}

				if (ex.GetType() == typeof(NonLogException))
				{
					uploadSet.CanTrack = false;
				}

				Console.WriteLine("Upload " + uploadSet.LoadNumber + " exception: " + ex.ToString());
			}
			finally
			{
				//Log if required
				if (uploadSet.CanTrack)
				{
					//log progress
					foreach (var item in uploadSet.UploadInfoList)
					{
						Logger.LogEvent(item.ToProperties());
					}

					//log dectivation 
					if (!String.IsNullOrEmpty(deActivationErrorMessage))
					{
						string subject = "Photo Booth Loading Error - Severity 2 - failed deactive photos";
						string error = deActivationErrorMessage + " endpoint:" + uploadSet.Setup.UploadEndpoint + " content:" + uploadSet.ToProperties();
						//SendEmail(uploadSet, subject, deActivationErrorMessage + );
						Logger.LogException(new Exception(deActivationErrorMessage));
					}
				}

				//remove from progress
				string l = uploadSet.LoadNumber;
				UploadFileQueue.Instance.InProgress.TryTake(out l);

			}

		}

		private void MoveFiles(UploadSet uploadSet, string[] loadFiles, bool isError = false)
		{
			string eventName = isError ? "ArchivingInvalidItemImages" : "ArchivingImages";


			//move files
			if (!String.IsNullOrEmpty(uploadSet.Setup.ArchiveFolder))
			{
				Dictionary<string, string> filesToMove = new Dictionary<string, string>();

				foreach (var item in loadFiles)
				{

					if (isError)
					{
						filesToMove.Add(item, item.ToErrorFolder(uploadSet.Setup.ArchiveFolder));
					}
					else
					{
						filesToMove.Add(item, item.ToDestinationFolder(uploadSet.Setup.ArchiveFolder));
					}
				}

				Logger.LogCustomEvent(eventName, uploadSet.ToProperties());

				MoveFiles(uploadSet.Setup.UserName, uploadSet.Setup.Password, uploadSet.Setup.FilePath, filesToMove);
			}
		}

		private string GetItemId(string loadNumber)
		{
			string itemId = String.Empty;
			string returnXml = String.Empty;

			try
			{
				Console.WriteLine("Calling PAS Web Service for load number: " + loadNumber + " " + DateTime.Now.ToString());

				PASService.n_ws_pas_interface svc = new PASService.n_ws_pas_interfaceClient();

				string svcParams = @"<notifyStockChange xmlns=""http://www.pickles.com.au/wsdl/StockBus/PasStock/elements"">";
				svcParams += @"<EventTypeCode>ItemDetailsForLoadNo</EventTypeCode>";
				svcParams += @"<SoaReferenceId>ItemDetailsTest001</SoaReferenceId>";
				svcParams += @"<LoadNo>" + loadNumber + "</LoadNo>";
				svcParams += "</notifyStockChange>";

				returnXml = svc.getdata(svcParams);

				//<? xml version = "1.0" encoding = "UTF-8" standalone = "no" ?>
				//< Items >< Item >< ItemId > 103139045 </ ItemId ></ Item ></ Items >
				XElement xe = XElement.Parse(returnXml);

				if (xe.Elements().Count() > 0)
				{
					itemId = xe.Elements().FirstOrDefault().Elements().FirstOrDefault().Value;
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine("PAS Web Service exception for load number: " + ex.ToString() + " " + DateTime.Now.ToString());
				Logger.LogException(new Exception("Response from PAS Web Service: " + returnXml));
				Logger.LogException(ex);
			}

			return itemId;
		}

		private bool FilesExist(string userName, string password, string path, List<string> files)
		{
			bool exists = true;

			string connectResult = String.Empty;

			//connect to share if username and password is required
			if (!String.IsNullOrEmpty(userName) && !String.IsNullOrEmpty(password))
			{
				connectResult = PInvokeWindowsNetworking.ConnectToRemote(path, userName, password);

				if (String.IsNullOrEmpty(connectResult))
				{
					//find all files from the folder
					foreach (var item in files)
					{
						if (!File.Exists(item))
						{
							exists = false;
							Logger.LogException(new Exception(item + " not found."));
						}
					}

					//disconnect
					connectResult = PInvokeWindowsNetworking.DisconnectRemote(path);

					if (String.IsNullOrEmpty(connectResult))
					{
						//let's not throw this exception as it does not matter for this service if it cannot disconnect
						//Logger.LogException(new Exception("Failed to disconnect from " + path + ". Error: " + connectResult));
					}

				}
				else
				{
					Logger.LogException(new Exception("Error connecting to " + path + ". Error: " + connectResult));
				}
			}
			else
			{
				//find all files from the folder
				foreach (var item in files)
				{
					if (!File.Exists(item))
					{
						exists = false;
						Logger.LogException(new Exception(item + " not found."));
					}
				}
			}

			return exists;
		}

		private void AddSeqToFiles(string userName, string password, string path, string searchPattern)
		{
			string[] files = new string[] { };
		
			string connectResult = String.Empty;

			//connect to share if username and password is required
			if (!String.IsNullOrEmpty(userName) && !String.IsNullOrEmpty(password))
			{
				connectResult = PInvokeWindowsNetworking.ConnectToRemote(path, userName, password);

				if (String.IsNullOrEmpty(connectResult))
				{
					//get all files from the folder
					files = Directory.GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);

					//disconnect
					connectResult = PInvokeWindowsNetworking.DisconnectRemote(path);

					if (String.IsNullOrEmpty(connectResult))
					{
						//let's not throw this exception as it does not matter for this service if it cannot disconnect
						//Logger.LogException(new Exception("Failed to disconnect from " + path + ". Error: " + connectResult));
					}

				}
				else
				{
					Logger.LogException(new Exception("Error connecting to " + path + ". Error: " + connectResult));
				}
			}
			else
			{
				//get all files from the folder
				files = Directory.GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
			}

			//only keep the files with the same numbe of fileLimits for each load
			Array.Sort(files);

			string[] filesForLoad = new string[] { };

			string totalPhotosString = String.Empty;

			processedFiles = Directory.GetFiles(path, loadNumber + "*_processed.jpg", SearchOption.TopDirectoryOnly);
			processedPhotos = processedFiles.Count();

			if (files.Count() > 0)
			{
				loadNumber = files[0].Substring(files[0].LastIndexOf('\\') + 1, files[0].IndexOf('_') - (files[0].LastIndexOf('\\') + 1));


				//find the complete file and get total photo count.
				completeFile = Directory.GetFiles(path, loadNumber + "*.ready", SearchOption.TopDirectoryOnly);

				filesForLoad = Directory.GetFiles(path, loadNumber + "*.jpg", SearchOption.TopDirectoryOnly);

				if (completeFile.Count() > 0)
				{

					totalPhotosString = completeFile[0].Substring(completeFile[0].ToLower().IndexOf("complete_") + 9,
																  completeFile[0].ToLower().IndexOf(".ready") - (completeFile[0].ToLower().IndexOf("complete_") + 9));

					if (totalPhotosString.Length > 0)
					{
						try
						{
							totalPhotos = Int32.Parse(totalPhotosString);
						}
						catch (FormatException e)
						{
							throw (e);
						}
					}
				}

				Dictionary<string, int> tempfileOrder = new Dictionary<string, int>();

				//add seq to file name
				for (int i = 0; i < filesForLoad.Count(); i++)
				{
					int sequenceNumber = Int32.Parse(GetSequenceNumber(filesForLoad[i].ToLower()));
					tempfileOrder.Add(filesForLoad[i].ToLower(), sequenceNumber);
				}

				//sort fileOrder
				tempfileOrder = tempfileOrder.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

				int processedIdx = 0;

				//reduce the gapse in fileOrder
				for (int index = 0; index < tempfileOrder.Count; index++)
				{
					var item = tempfileOrder.ElementAt(index);
					var itemKey = item.Key;
					var itemValue = item.Value;
					
					if (itemKey.Contains("_processed"))
					{
						processedIdx = index + 1;
					}

					if (itemValue >= index + 1)
					{
						fileOrder.Add(itemKey.ToLower(), (index - processedIdx) + 1);
					}
					else
					{
						fileOrder.Add(itemKey.ToLower(), itemValue);
					}
				}


				//copy  key into a array
				loadFiles = fileOrder.Keys.ToArray();
			}

		}


		private string[] GetFiles(int fileLimits)
		{
			int count = 0;
			var newFiles = new List<string>();

			for (int i = 0; i < loadFiles.Count(); i++)
			{
				if (count < fileLimits &&  !loadFiles[i].Contains("_processed") )
				{
					newFiles.Add(loadFiles[i]);
					count++;
				}
			}

			processFiles = newFiles.ToArray();

			return processFiles; 

		}

		
		private void MoveFiles(string userName, string password, string path, Dictionary<string, string> files)
		{
			try
			{
				string connectResult = String.Empty;

				//connect to share if username and password is required
				if (!String.IsNullOrEmpty(userName) && !String.IsNullOrEmpty(password))
				{
					connectResult = PInvokeWindowsNetworking.ConnectToRemote(path, userName, password);

					if (String.IsNullOrEmpty(connectResult))
					{
						foreach (var item in files)
						{
							Console.WriteLine("Moving  " + item.Key + " to " + item.Value);

							//create directory if required
							DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(item.Value));

							if (!di.Exists)
							{
								di.Create();
							}

							if (File.Exists(item.Value))
							{
								File.Delete(item.Value);
							}
							File.Move(item.Key, item.Value);
						}
						//disconnect
						connectResult = PInvokeWindowsNetworking.DisconnectRemote(path);

						if (String.IsNullOrEmpty(connectResult))
						{
							//let's not throw this exception as it does not matter for this service if it cannot disconnect
							//throw (new FileArchiveException("Failed to disconnect from " + path + ". Error: " + connectResult));
						}

					}
					else
					{
						throw (new FileArchiveException("Error connecting to " + path + ". Error: " + connectResult));
					}
				}
				else
				{
					foreach (var item in files)
					{
						Console.WriteLine("Moving  " + item.Key + " to " + item.Value);

						//create directory if required
						DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(item.Value));

						if (!di.Exists)
						{
							di.Create();
						}

						if (File.Exists(item.Value))
						{
							File.Delete(item.Value);
						}
						File.Move(item.Key, item.Value);
					}
				}
			}
			catch (Exception ex)
			{
				throw new FileArchiveException(ex.ToString());
			}

		}

		private List<byte[]> GetImageData(string userName, string password, string path, List<UploadInfo> files)
		{
			List<byte[]> imageData = new List<byte[]>();

			string connectResult = String.Empty;

			//connect to share if username and password is required
			if (!String.IsNullOrEmpty(userName) && !String.IsNullOrEmpty(password))
			{
				connectResult = PInvokeWindowsNetworking.ConnectToRemote(path, userName, password);

				if (String.IsNullOrEmpty(connectResult))
				{
					foreach (var item in files)
					{
						imageData.Add(File.ReadAllBytes(item.FileName));
					}

					//disconnect
					connectResult = PInvokeWindowsNetworking.DisconnectRemote(path);

					if (String.IsNullOrEmpty(connectResult))
					{
						//let's not throw this exception as it does not matter for this service if it cannot disconnect
						//Logger.LogException(new Exception("Failed to disconnect from " + path + ". Error: " + connectResult));
					}
				}
				else
				{
					Logger.LogException(new Exception("Error connecting to " + path + ". Error: " + connectResult));
				}
			}
			else
			{
				foreach (var item in files)
				{
					imageData.Add(File.ReadAllBytes(item.FileName));
				}
			}

			return imageData;
		}

		private void SendEmail(UploadSet uploadSet, string subject, string body)
		{

			using (var httpClient = new HttpClient())
			{
				httpClient.Timeout = TimeSpan.FromMinutes(uploadSet.Setup.UploadTimeoutMinutes);
				//httpClient.BaseAddress = new Uri(uploadSet.Setup.EmailEndpoint);

				using (var content = new MultipartFormDataContent())
				{
					Console.WriteLine("Deactivating images");

					Dictionary<string, string> svcMetadata = new Dictionary<string, string>();

					svcMetadata.Clear();

					var jsonData = "{" + String.Join(",", svcMetadata.Values) + "}";

					content.Add(new StringContent("update"), "action");
					content.Add(new StringContent(CurrentEnvironment.UserName), "transactionUser");
					content.Add(new StringContent(jsonData), "images");

					Logger.LogCustomEvent("sendEmail", svcMetadata);

					/*
					//var response = httpClient.PostAsync(uploadSet.Setup.EmailEndPoint, content).Result;

					if (response.StatusCode != HttpStatusCode.OK)
					{
						var reason = response.ReasonPhrase;
						//response.Content.ReadAsStringAsync().Result;
					}
					*/
				}
			}

		}

		private string GetUsage(string fileName)
		{
			string sequence = fileName.Substring(fileName.IndexOf("_") + 1, fileName.IndexOf(".jpg") - fileName.IndexOf("_") - 1);
			string usage = "";

			if (sequence == "open_13") 
			{
				usage = "Web Condition,";

				for (int i = 0; i < loadFiles.Count(); i++) 
				{
					if (loadFiles[i].Contains("dash_01"))
					{
						usage = "";
						break;
					}
				}
			}

			if (sequence == "open_12")
			{
				usage = "Pickles Live,";

				for (int i = 0; i < loadFiles.Count(); i++)
				{
					
					if (loadFiles[i].Contains("driverfront_01"))
					{
						usage = "";
						break;
					}

				}
			}

			if (sequence == "open_06")
			{
				usage = "Pickles Live,";

				for (int i = 0; i < loadFiles.Count(); i++)
				{
					if (loadFiles[i].Contains("passengerfront_01"))
					{
						usage = "";
						break;
					}

				}
			}

			return usage;
		}


		private string GetSequenceNumber(string fileName)
		{
			int sequenceNumber = 0;
			string sequence = fileName.Substring(fileName.IndexOf("_") + 1, fileName.IndexOf(".jpg") - fileName.IndexOf("_")-1 );
			
			if (fileName.ToLower().Contains("other"))
			{
				GetSequenceMapping().TryGetValue("other", out sequenceNumber);

				sequenceNumber = sequenceNumber + Int32.Parse(sequence.Substring(sequence.IndexOf("_") + 1, 2));

			}
			else
			{
				GetSequenceMapping().TryGetValue(sequence.ToLower(), out sequenceNumber);
			}


			return sequenceNumber.ToString();
		}


			private Dictionary<String, int> GetSequenceMapping()
		{
			Dictionary<string, int> seqMapping = new Dictionary<string, int>();

			seqMapping.Add("hero_01", 15);
			seqMapping.Add("hero_02", 14);
			seqMapping.Add("hero_03", 13);
			seqMapping.Add("hero_04", 12);
			seqMapping.Add("hero_05", 11);
			seqMapping.Add("hero_06", 10);
			seqMapping.Add("hero_07", 9);
			seqMapping.Add("hero_08", 8);
			seqMapping.Add("hero_09", 7);
			seqMapping.Add("hero_10", 6);
			seqMapping.Add("hero_11", 5);
			seqMapping.Add("hero_12", 4);
			seqMapping.Add("hero_13", 3);
			seqMapping.Add("hero_14", 2);
			seqMapping.Add("hero_15", 1);
			seqMapping.Add("hero_16", 16);
			
			//open
			seqMapping.Add("open_01", 31);
			seqMapping.Add("open_02", 30);
			seqMapping.Add("open_03", 29);
			seqMapping.Add("open_04", 28);
			seqMapping.Add("open_05", 27);
			seqMapping.Add("open_06", 26);
			seqMapping.Add("open_07", 25);
			seqMapping.Add("open_08", 24);
			seqMapping.Add("open_09", 23);
			seqMapping.Add("open_10", 22);
			seqMapping.Add("open_11", 21);
			seqMapping.Add("open_12", 20);
			seqMapping.Add("open_13", 19);
			seqMapping.Add("open_14", 18);
			seqMapping.Add("open_15", 17);
			seqMapping.Add("open_16", 32);

			seqMapping.Add("dash_01", 33);
			seqMapping.Add("driverfront_01", 34);
			seqMapping.Add("passengerfront_01", 35);
			seqMapping.Add("driverrear_01", 36);
			seqMapping.Add("passengerrear_01", 37);

			seqMapping.Add("other", 38);

			seqMapping.Add("high_01", 137);
			seqMapping.Add("high_02", 138);
			seqMapping.Add("topdown_01", 139);

			return seqMapping;
		}
	}
}
