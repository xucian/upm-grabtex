using Cysharp.Threading.Tasks;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using WebP;

namespace com.xucian.upm.grabtex
{
	public class GrabTex
	{
		public async UniTask IntoAsync(string url, RawImage into, CancellationToken cancellation)
		{
			var tex = await Async(url, cancellation);
			if (!tex)
				return;

			if (!into || cancellation.IsCancellationRequested)
			{
				UnityEngine.Object.Destroy(tex);
				return;
			}

			into.texture = tex;
		}

		public async UniTask<Texture2D> Async(string url, CancellationToken cancellation)
		{
			var imgInfo = await FindImageUrlAndContentTypeAsync(url, cancellation);
			if (imgInfo.url == null || cancellation.IsCancellationRequested)
				return null;

			return await DownloadImageAsync(imgInfo.url, imgInfo.contentType, cancellation);
		}

		async UniTask<(string url, string contentType)> FindImageUrlAndContentTypeAsync(string url, CancellationToken cancellation)
		{
			var contentType = await GuessRealContentTypeAsync(url);
			string imageContentType;
			string imageUrl;

			if (contentType.StartsWith("image/"))
			{
				imageContentType = contentType;
				imageUrl = url;
			}
			else if (contentType.StartsWith("text/html"))
			{
				imageUrl = await FindImageInHtmlAsync(url, cancellation);

				if (imageUrl == null)
					return (null, null);

				if (cancellation.IsCancellationRequested)
					return (null, null);

				imageContentType = await GuessRealContentTypeAsync(imageUrl);
			}
			else
			{
				return (null, null);
			}

			if (cancellation.IsCancellationRequested)
				return (null, null);

			return (imageUrl, imageContentType);
		}

		async UniTask<Texture2D> DownloadImageAsync(string url, string contentType, CancellationToken cancellation)
		{
			contentType ??= await GuessRealContentTypeAsync(url);

			if (contentType.StartsWith("image/webp") || url.EndsWith(".webp"))
				return await DownloadWebpImageAsync(url, cancellation);

			return await DownloadRegularImageAsync(url, cancellation);
		}

		async UniTask<string> FindImageInHtmlAsync(string url, CancellationToken cancellation)
		{
			using (var req = UnityWebRequest.Get(url))
			{
				SetRequestHeaders(req);
				await req.SendWebRequest();

				if (req.result != UnityWebRequest.Result.Success)
				{
					Debug.Log("Error: " + req.error);
					return null;
				}

				string pageContent = req.downloadHandler.text;
				return ParseHtmlForOGImage(pageContent);
			}
		}

		string ParseHtmlForOGImage(string htmlContent)
		{
			var match = Regex.Match(htmlContent, "<meta property=\"og:image\" content=\"(.*?)\"");
			if (!match.Success)
				return null;

			var ogImageUrl = match.Groups[1].Value;
			return ogImageUrl;
		}

		async UniTask<Texture2D> DownloadRegularImageAsync(string url, CancellationToken cancellation)
		{
			using (var req = UnityWebRequestTexture.GetTexture(url))
			{
				await SendRequest(req, cancellation);
				if (cancellation.IsCancellationRequested)
					return null;

				return DownloadHandlerTexture.GetContent(req);
			}
		}

		async UniTask<Texture2D> DownloadWebpImageAsync(string url, CancellationToken cancellation)
		{
			using (var req = UnityWebRequest.Get(url))
			{
				SetRequestHeaders(req);
				await SendRequest(req, cancellation);
				if (cancellation.IsCancellationRequested)
					return null;

				return CreateTextureFromWebpRequest(req);
			}
		}

		async UniTask SendRequest(UnityWebRequest req, CancellationToken cancellation)
		{
			await req.SendWebRequest();

			if (cancellation.IsCancellationRequested)
				return;

			if (req.result != UnityWebRequest.Result.Success)
				Debug.Log("Error: " + req.error);
		}

		async UniTask<string> GuessRealContentTypeAsync(string url)
		{
			// Prioritize the server's returned MIME type
			var ct = await GetContentTypeAsync(url);
			if (ct.StartsWith("image/"))
				return ct;

			// Remove the query part
			int queryCharIdx = url.IndexOf('?');
			if (queryCharIdx != -1)
				url = url[..queryCharIdx];

			if (url.EndsWith(".webp"))
				return "image/webp";

			if (url.EndsWith(".gif"))
				return "image/gif";

			if (url.EndsWith(".jpeg") || url.EndsWith(".jpg"))
				return "image/jpeg";

			if (url.EndsWith(".bmp"))
				return "image/bmp";

			return ct;
		}

		async UniTask<string> GetContentTypeAsync(string url)
		{
			using (var req = UnityWebRequest.Head(url))
			{
				SetRequestHeaders(req);
				await req.SendWebRequest();
				return req.GetResponseHeader("Content-Type");
			}
		}

		void SetRequestHeaders(UnityWebRequest req)
		{
			req.SetRequestHeader("User-Agent", "com.xucian.upm.grabtex");  // some websites return "403 forbidden" if no User-Agent header
		}

		Texture2D CreateTextureFromWebpRequest(UnityWebRequest req)
		{
			var bytes = req.downloadHandler.data;
			var texture = Texture2DExt.CreateTexture2DFromWebP(bytes, lMipmaps: true, lLinear: true, lError: out Error lError);
			if (lError != Error.Success)
			{
				Debug.Log("Webp Load Error : " + lError.ToString());
				return null;
			}

			return texture;
		}
	}
}
