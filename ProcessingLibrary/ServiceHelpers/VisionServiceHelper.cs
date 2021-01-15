using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ServiceHelpers
{
    public static class VisionServiceHelper
    {
        public static int RetryCountOnQuotaLimitError = 10;
        public static int RetryDelayOnQuotaLimitError = 500;

        private static ComputerVisionClient visionClient { get; set; }

        static VisionServiceHelper()
        {
            InitializeVisionService();
        }

        public static Action Throttled;

        private static string apiKey;
        public static string ApiKey
        {
            get
            {
                return apiKey;
            }

            set
            {
                var changed = apiKey != value;
                apiKey = value;
                if (changed)
                {
                    InitializeVisionService();
                }
            }
        }

        private static string url;
        public static string Url
        {
            get
            {
                return url;
            }

            set
            {
                var changed = url != value;
                url = value;
                if (changed)
                {
                    InitializeVisionService();
                }
            }
        }

        private static void InitializeVisionService()
        {
            visionClient = new ComputerVisionClient(new ApiKeyServiceClientCredentials(apiKey)) {
                Endpoint = url
            };
        }

        // handle throttling issues
        private static async Task<TResponse> RunTaskWithAutoRetryOnQuotaLimitExceededError<TResponse>(Func<Task<TResponse>> action)
        {
            int retriesLeft = VisionServiceHelper.RetryCountOnQuotaLimitError;
            int delay = VisionServiceHelper.RetryDelayOnQuotaLimitError;

            TResponse response = default(TResponse);

            while (true)
            {
                try
                {
                    response = await action();
                    break;
                }
                catch (Exception exception) when (retriesLeft > 0)
                {
                    ErrorTrackingHelper.TrackException(exception, "Vision API throttling error");
                    if (retriesLeft == 1 && Throttled != null)
                    {
                        Throttled();
                    }

                    await Task.Delay(delay);
                    retriesLeft--;
                    delay *= 2;
                    continue;
                }
            }

            return response;
        }

        // Pull in the methods to call
        private static async Task RunTaskWithAutoRetryOnQuotaLimitExceededError(Func<Task> action)
        {
            await RunTaskWithAutoRetryOnQuotaLimitExceededError<object>(async () => { await action(); return null; });
        }

        public static async Task<ImageAnalysis> AnalyzeImageAsync(string imageUrl, IList<VisualFeatureTypes?> visualFeatures = null, IList<Details?> details = null)
        {
            return await visionClient.AnalyzeImageAsync(imageUrl, visualFeatures, details);
            // return await RunTaskWithAutoRetryOnQuotaLimitExceededError<ImageAnalysis>(() => visionClient.AnalyzeImageAsync(imageUrl, visualFeatures, details));
        }


    }
}
