﻿using System.Net.Http;
using Silk.Shared.Constants;

namespace Silk.Core.Utilities
{
    public static class HttpClientExtensions
    {
        public static HttpClient CreateSilkClient(this IHttpClientFactory httpClientFactory)
        {
            return httpClientFactory.CreateClient(StringConstants.HttpClientName);
        }
    }
}