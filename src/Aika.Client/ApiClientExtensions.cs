using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Aika.Client {
    /// <summary>
    /// HTTP request extension methods.
    /// </summary>
    internal static class ApiClientExtensions {

        /// <summary>
        /// Media type of JSON payloads.
        /// </summary>
        public const string JsonMediaType = "application/json";


        /// <summary>
        /// Performs an HTTP GET request.
        /// </summary>
        /// <param name="client">The API client to use.</param>
        /// <param name="url">The request URI.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the HTTP response message.
        /// </returns>
        internal static Task<HttpResponseMessage> GetAsync(this ApiClient client, string url, CancellationToken cancellationToken) {
            return SendAsync(client, HttpMethod.Get, url, null, cancellationToken);
        }


        /// <summary>
        /// Sends the specified content as JSON to the specified URL using an HTTP POST request.
        /// </summary>
        /// <typeparam name="T">The type of the content</typeparam>
        /// <param name="client">The API client to use.</param>
        /// <param name="url">The request URI.</param>
        /// <param name="content">The content to send.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the HTTP response message.
        /// </returns>
        internal static Task<HttpResponseMessage> PostAsJsonAsync<T>(this ApiClient client, string url, T content, CancellationToken cancellationToken) {
            return SendAsJsonAsync(client, HttpMethod.Post, url, content, cancellationToken);
        }


        /// <summary>
        /// Sends the specified content as JSON to the specified URL using an HTTP PUT request.
        /// </summary>
        /// <typeparam name="T">The type of the content</typeparam>
        /// <param name="client">The API client to use.</param>
        /// <param name="url">The request URI.</param>
        /// <param name="content">The content to send.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the HTTP response message.
        /// </returns>
        internal static Task<HttpResponseMessage> PutAsJsonAsync<T>(this ApiClient client, string url, T content, CancellationToken cancellationToken) {
            return SendAsJsonAsync(client, HttpMethod.Put, url, content, cancellationToken);
        }


        /// <summary>
        /// Sends the specified content as JSON to the specified URL.
        /// </summary>
        /// <typeparam name="T">The type of the content</typeparam>
        /// <param name="client">The API client to use.</param>
        /// <param name="method">The HTTP method to use.</param>
        /// <param name="url">The request URI.</param>
        /// <param name="content">The content to send.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the HTTP response message.
        /// </returns>
        private static Task<HttpResponseMessage> SendAsJsonAsync<T>(ApiClient client, HttpMethod method, string url, T content, CancellationToken cancellationToken) {
            return SendAsync(client, method, url, new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, JsonMediaType), cancellationToken);
        }


        /// <summary>
        /// Sends an HTTP request
        /// </summary>
        /// <param name="client">The API client to use.</param>
        /// <param name="method">The HTTP method to use.</param>
        /// <param name="url">The request URI.</param>
        /// <param name="content">The content to send.  Can be <see langword="null"/>.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the HTTP response message.
        /// </returns>
        private static Task<HttpResponseMessage> SendAsync(ApiClient client, HttpMethod method, string url, HttpContent content, CancellationToken cancellationToken) {
            if (client == null) {
                throw new ArgumentNullException(nameof(client));
            }

            var message = new HttpRequestMessage(method, url);
            if (content != null) {
                message.Content = content;
            }

            return client.HttpClient.SendAsync(message, cancellationToken);
        }


        /// <summary>
        /// Deserializes the specified HTTP content containing a JSON payload.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the content to.</typeparam>
        /// <param name="content">The content.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the deserialized content.
        /// </returns>
        internal static async Task<T> ReadAsJsonAsync<T>(this HttpContent content, CancellationToken cancellationToken) {
            if (content == null) {
                throw new ArgumentNullException(nameof(content));
            }

            if (!JsonMediaType.Equals(content.Headers.ContentType.MediaType)) {
                throw new ArgumentException($"Content type must be {JsonMediaType}!", nameof(content));
            }

            var json = await content.ReadAsStringAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            return JsonConvert.DeserializeObject<T>(json);
        }


        /// <summary>
        /// Performs an HTTP DELETE request.
        /// </summary>
        /// <param name="client">The API client to use.</param>
        /// <param name="url">The request URI.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>
        /// A task that will return the HTTP response message.
        /// </returns>
        internal static Task<HttpResponseMessage> DeleteAsync(this ApiClient client, string url, CancellationToken cancellationToken) {
            return SendAsync(client, HttpMethod.Delete, url, null, cancellationToken);
        }

    }
}
