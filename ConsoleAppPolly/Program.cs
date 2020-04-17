using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleAppPolly
{
    class Program
    {
        private static readonly string API_URL = "http://date.jsontest.com/";
        private static int currNumReq = 0;
        private static int maxNumRetry = 3;

        private enum PolicyEnum
        {
            NoPolicy = 1,
            RetryPolicy,
            WaitAndRetryPolicy,
            FallbackPolicy,
            WrapPolicy
        };

        static void Main(string[] args)
        {
            Console.WriteLine("Policy example program.");
            var anotherOne = string.Empty;
            do
            {
                Console.WriteLine(string.Empty);
                Console.WriteLine("Choose the type of policy to show:\n");
                foreach (PolicyEnum p in Enum.GetValues(typeof(PolicyEnum)))
                {
                    Console.WriteLine($"{(int)p}. {p}");
                }
                Console.WriteLine(string.Empty);
                var input = Console.ReadLine();

                if (int.TryParse(input, out int res))
                {
                    Console.WriteLine(string.Empty);

                    var policy = (PolicyEnum)Enum.Parse(typeof(PolicyEnum), input);
                    switch (policy)
                    {
                        case PolicyEnum.NoPolicy:
                            NoPolicyExample();
                            break;
                        case PolicyEnum.RetryPolicy:
                            RetryPolicyExample();
                            break;
                        case PolicyEnum.WaitAndRetryPolicy:
                            WaitAndRetryPolicy();
                            break;
                        case PolicyEnum.FallbackPolicy:
                            FallbackPolicy();
                            break;
                        case PolicyEnum.WrapPolicy:
                            WrapPolicy();
                            break;
                        default:
                            Console.WriteLine("Invalid number.");
                            break;
                    }
                }

                Console.WriteLine(string.Empty);
                Console.WriteLine("Do you want to choose another policy? Y/(N)");
                input = Console.ReadLine();
                anotherOne = string.IsNullOrWhiteSpace(input) ? "N" : input;
            } while (anotherOne.Equals("Y", StringComparison.InvariantCultureIgnoreCase));

            Console.WriteLine(string.Empty);
            Console.WriteLine("Program finished");
            Console.Read();
        }

        private static void WrapPolicy()
        {
            Console.WriteLine("Calling Api wrapping two policies: fallback and waitAndRetry policy");
            try
            {
                var fallBackPolicy = Policy<string>.Handle<Exception>().Fallback(
                     "Try again later (message from fallback policy)",
                    (res) =>
                    {
                        Console.WriteLine($"Exception catched from the fallback policy handler");
                    }
                );

                var waitAndRetryPolicy = Policy.Handle<Exception>().WaitAndRetry(
                    3, //max number of retry
                    attempt => TimeSpan.FromSeconds(0.1 * Math.Pow(2, attempt)), //2,4,8
                    (exception, calculatedWaitDuration) =>
                    {

                        Console.WriteLine($"Exception catched from the waitAndRetry policy handler");
                        Console.WriteLine($"Waited for: {calculatedWaitDuration.Milliseconds}ms");
                    }
                );

                var wrapPolicy = fallBackPolicy.Wrap(waitAndRetryPolicy);

                var resp = wrapPolicy.Execute(() =>
                {
                    CallApiEndpoint();
                    return "Ok";
                });
                Console.WriteLine($"resp: {resp}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception! Err msg: {ex.Message}");
            }
        }

        private static void FallbackPolicy()
        {
            Console.WriteLine("Calling Api with fallbak policy (a simple message from the policy)");
            try
            {
                var policy = Policy<string>.Handle<Exception>().Fallback(
                     "Try again later (message from fallback policy)",
                    (res) =>
                    {
                        Console.WriteLine($"Exception catched from the policy handler");
                    });

                var resp = policy.Execute(() =>
                {
                    CallApiEndpoint();
                    return "Ok";
                });
                Console.WriteLine($"resp: {resp}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception! Err msg: {ex.Message}");
            }
        }

        private static void WaitAndRetryPolicy()
        {
            Console.WriteLine("Calling Api with wait and retry policy (max 3 retry, exponential backoff times)");
            try
            {
                var policy = Policy.Handle<Exception>().WaitAndRetry(
                    3, //max number of retry
                    attempt => TimeSpan.FromSeconds(0.1 * Math.Pow(2, attempt)), //2,4,8
                    (exception, calculatedWaitDuration) =>
                    {

                        Console.WriteLine($"Exception catched from the policy handler");
                        Console.WriteLine($"Waited for: {calculatedWaitDuration.Milliseconds}ms");
                    });

                policy.Execute(() =>
                {
                    CallApiEndpoint();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception! Err msg: {ex.Message}");
            }
        }

        private static void RetryPolicyExample()
        {
            Console.WriteLine("Calling Api with retry policy (max 3 retry)");
            try
            {
                var policy = Policy.Handle<Exception>().Retry(3, (exception, attempt) =>
                {
                    Console.WriteLine($"Exception catched from the policy handler, attempt {attempt}");
                });

                policy.Execute(() =>
                {
                    CallApiEndpoint();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception! Err msg: {ex.Message}");
            }
        }

        private static void NoPolicyExample()
        {
            Console.WriteLine($"Calling Api Without policy");
            try
            {
                CallApiEndpoint();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception! Err msg: {ex.Message}");
            }
        }

        private static void CallApiEndpoint()
        {
            for (currNumReq = 0; currNumReq < maxNumRetry; currNumReq++)
            {
                Console.WriteLine($"currNumReq {currNumReq}");
                using (var client = new WebClient())
                {
                    var response = client.DownloadString(API_URL);
                }

                if (currNumReq == 1)
                {
                    throw new OperationCanceledException();
                }

                Thread.Sleep(500);
            }
        }
    }
}
