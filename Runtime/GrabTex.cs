using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
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
		readonly Dictionary<string, string> EXTENSION_TO_MIME = new()
		{
			{".webp", "image/webp"},
			{".gif",  "image/gif"},
			{".jpeg", "image/jpeg"},
			{".jpg",  "image/jpeg"},
			{".bmp",  "image/bmp"}
		};


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

				string html = req.downloadHandler.text;
				return ParseHtmlForOGImage(html) ?? ParseHtmlForFirstSupportedImage(html);
			}
		}

		string ParseHtmlForOGImage(string html)
		{
			// Building a pattern to match any of the given extensions
			var extensionsPattern = string.Join('|', EXTENSION_TO_MIME.Keys);
			var pattern = $"<meta property=\"og:image\" content=\"(.*?({extensionsPattern}))(?:\\?[^\"']*)?\"";

			var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
			if (!match.Success)
				return null;

			// Use Groups[1].Value to get the content of the `content` attribute
			var ogImageUrl = match.Groups[1].Value;
			return ogImageUrl;
		}

		string ParseHtmlForFirstSupportedImage(string htmlContent)
		{
			// Building a pattern to match any of the given extensions
			var extensionsPattern = string.Join('|', EXTENSION_TO_MIME.Keys);
			var pattern = $"<img[^>]*?\\s?src=\"(.*?({extensionsPattern}))(?:\\?[^\"']*)?\"";

			var match = Regex.Match(htmlContent, pattern, RegexOptions.IgnoreCase);
			if (!match.Success)
				return null;

			var imageUrl = match.Groups[1].Value;
			return imageUrl;
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
			//try
			//{
			await req.SendWebRequest();
			//}
			//catch (Exception e)
			//{
			//	Debug.LogError(req.downloadHandler.error);
			//}

			if (cancellation.IsCancellationRequested)
				return;

			if (req.result != UnityWebRequest.Result.Success)
				Debug.Log("Error: " + req.error);
		}

		async UniTask<string> GuessRealContentTypeAsync(string url)
		{
			// Prioritize the server's returned MIME type
			string serverReturnedCt = null;
			try 
			{
				var ct = await GetContentTypeAsync(url);
				if (ct.StartsWith("image/"))
					return ct;

				serverReturnedCt = ct;
			}
			catch (UnityWebRequestException)
			{
				// Some sites return 404 on HEAD requests, but we can still assume they're valid html, and we'll be right more often than we'll be wrong
				serverReturnedCt = "text/html";
			}

			// Remove the query part
			int queryCharIdx = url.IndexOf('?');
			if (queryCharIdx != -1)
				url = url[..queryCharIdx];

			// Prioritize extension over returned CT
			foreach (var item in EXTENSION_TO_MIME)
			{
				if (url.EndsWith(item.Key))
					return item.Value;
			}

			return serverReturnedCt;
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
			req.SetRequestHeader("User-Agent", "Homemade Browser with Love");  // some websites return "403 forbidden" if no User-Agent header
			req.SetRequestHeader("Accept", "*/*");  // some websites return "404 not found" if no Accept header
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
