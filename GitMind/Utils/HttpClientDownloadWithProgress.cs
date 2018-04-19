using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;


namespace GitMind.Utils
{
	public class HttpClientDownloadWithProgress : IDisposable
	{
		public HttpClient HttpClient { get; } = new HttpClient();


		public delegate void ProgressChangedHandler(
			long? totalFileSize, long totalBytesDownloaded, double? progressPercentage);

		public event ProgressChangedHandler ProgressChanged;


		public async Task StartDownload(string uri, string filePath)
		{
			ServicePointManager.Expect100Continue = true;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
		
			using (var response = await HttpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead))
			{
				await DownloadFile(response, filePath);
			}
		}


		public void Dispose() => HttpClient?.Dispose();


		private async Task DownloadFile(HttpResponseMessage response, string filePath)
		{
			response.EnsureSuccessStatusCode();

			long? totalBytes = response.Content.Headers.ContentLength;

			using (Stream contentStream = await response.Content.ReadAsStreamAsync())
			{
				await ProcessContentStream(totalBytes, contentStream, filePath);
			}
		}


		private async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream, string filePath)
		{
			long totalBytesRead = 0L;
			long readCount = 0L;
			byte[] buffer = new byte[8192];
			bool isMoreToRead = true;

			using (var fileStream = new FileStream(
				filePath, FileMode.Create, FileAccess.Write, FileShare.None, buffer.Length, true))
			{
				do
				{
					var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
					if (bytesRead == 0)
					{
						isMoreToRead = false;
						TriggerProgressChanged(totalDownloadSize, totalBytesRead);
						continue;
					}

					await fileStream.WriteAsync(buffer, 0, bytesRead);

					totalBytesRead += bytesRead;
					readCount += 1;

					if (readCount % 100 == 0)
					{
						TriggerProgressChanged(totalDownloadSize, totalBytesRead);
					}
				}
				while (isMoreToRead);
			}
		}


		private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
		{
			if (ProgressChanged == null)
			{
				return;
			}

			double? progressPercentage = null;
			if (totalDownloadSize.HasValue)
			{
				progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2);
			}

			ProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
		}
	}
}