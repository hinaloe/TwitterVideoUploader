using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoreTweet;
using CoreTweet.Core;
using CoreTweet.Rest;
using TwVideoUp.util;

namespace TwVideoUp.Core
{
    public partial class MediaAsyncExtend
    {
        public MediaAsyncExtend(Tokens token)
        {
            coreTweeTokens = token;
        }

        private Tokens coreTweeTokens;

        public Task<UploadFinalizeCommandResult> UploadChunkedAsync(Stream media, long totalBytes, UploadMediaType mediaType,
            IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellationToken)
        {
            return coreTweeTokens.Media.UploadInitCommandAsync(parameters.EndWith(
                new KeyValuePair<string, object>("total_bytes", totalBytes),
                new KeyValuePair<string, object>("media_type", mediaType)
                ), cancellationToken)
                .Done(result =>
                {
                    const int maxChunkSize = 5*1000*1000;
                    var tasks = new List<Task>((int) (totalBytes/maxChunkSize) + 1);
                    var sem = new Semaphore(2, 2);
                    var remainingBytes = totalBytes;

                    for (var segmentIndex = 0; remainingBytes > 0; segmentIndex++)
                    {
                        sem.WaitOne();
                        var chunkSize = (int) Math.Min(remainingBytes, maxChunkSize);
                        var chunk = new byte[chunkSize];
                        var readCount = media.Read(chunk, 0, chunkSize);
                        if (readCount == 0) break;
                        remainingBytes -= readCount;
                        tasks.Add(
                            coreTweeTokens.Media.UploadAppendCommandAsync(
                                new Dictionary<string, object>
                                {
                                    {"media_id", result.MediaId},
                                    {"segment_index", segmentIndex},
                                    {"media", new ArraySegment<byte>(chunk, 0, readCount)}
                                },
                                cancellationToken
                                ).ContinueWith(t =>
                                {
                                    sem.Release();
                                    return t;
                                }).Unwrap()
                            );
                    }
                    return Task.WhenAll(tasks)
                        .Done(() => coreTweeTokens.Media.UploadFinalizeCommandAsync(result.MediaId, cancellationToken),
                            cancellationToken)
                        .Unwrap();
                }, cancellationToken, true).Unwrap();
        }

        private Task<MediaUploadResult> WaitForProcessing(long mediaId, CancellationToken cancellationToken)
        {
            return coreTweeTokens.Media.UploadStatusCommandAsync(mediaId, cancellationToken)
                .Done(x =>
                {
                    if (x.ProcessingInfo?.State == "failed")
                        throw new MediaProcessingException(x);

                    if (x.ProcessingInfo?.CheckAfterSecs != null)
                    {
                        return InternalUtils.Delay(x.ProcessingInfo.CheckAfterSecs.Value*1000, cancellationToken)
                            .Done(() => this.WaitForProcessing(mediaId, cancellationToken), cancellationToken)
                            .Unwrap();
                    }

                    return Task.FromResult<MediaUploadResult>(x);
                }, cancellationToken)
                .Unwrap();
        }
    }

    internal static class EnumerableExtensions
    {
        internal static IEnumerable<string> EnumerateLines(this StreamReader streamReader)
        {
            while (!streamReader.EndOfStream)
                yield return streamReader.ReadLine();
        }

        internal static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T item in source)
                action(item);
        }

        internal static string JoinToString<T>(this IEnumerable<T> source)
        {
#if !NET35
            return string.Concat(source);
#else
            return string.Concat(source.Cast<object>().ToArray());
#endif
        }

        internal static string JoinToString<T>(this IEnumerable<T> source, string separator)
        {
#if !NET35
            return string.Join(separator, source);
#else
            return string.Join(separator, source.Select(x => x.ToString()).ToArray());
#endif
        }

        internal static IEnumerable<T> EndWith<T>(this IEnumerable<T> source, params T[] second)
        {
            return source.Concat(second);
        }
    }

    internal struct Unit
    {
        internal static readonly Unit Default = new Unit();
    }

    internal static class TaskExtensions
    {
        internal static Task<TResult> Done<TSource, TResult>(this Task<TSource> source, Func<TSource, TResult> action,
            CancellationToken cancellationToken, bool longRunning = false)
        {
            var tcs = new TaskCompletionSource<TResult>();
            source.ContinueWith(t =>
            {
                if (t.IsCanceled || cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled();
                    return;
                }

                if (t.Exception != null)
                {
                    tcs.TrySetException(t.Exception.InnerExceptions.Count == 1
                        ? t.Exception.InnerException
                        : t.Exception);
                    return;
                }

                try
                {
                    tcs.TrySetResult(action(t.Result));
                }
                catch (OperationCanceledException)
                {
                    tcs.TrySetCanceled();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, longRunning ? TaskContinuationOptions.LongRunning : TaskContinuationOptions.ExecuteSynchronously);
            return tcs.Task;
        }

        internal static Task Done<TSource>(this Task<TSource> source, Action<TSource> action,
            CancellationToken cancellationToken, bool longRunning = false)
        {
            var tcs = new TaskCompletionSource<Unit>();
            source.ContinueWith(t =>
            {
                if (t.IsCanceled || cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled();
                    return;
                }

                if (t.Exception != null)
                {
                    tcs.TrySetException(t.Exception.InnerExceptions.Count == 1
                        ? t.Exception.InnerException
                        : t.Exception);
                    return;
                }

                try
                {
                    action(t.Result);
                    tcs.TrySetResult(Unit.Default);
                }
                catch (OperationCanceledException)
                {
                    tcs.TrySetCanceled();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, longRunning ? TaskContinuationOptions.LongRunning : TaskContinuationOptions.ExecuteSynchronously);
            return tcs.Task;
        }

        internal static Task<TResult> Done<TResult>(this Task source, Func<TResult> action,
            CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<TResult>();
            source.ContinueWith(t =>
            {
                if (t.IsCanceled || cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled();
                    return;
                }

                if (t.Exception != null)
                {
                    tcs.TrySetException(t.Exception.InnerExceptions.Count == 1
                        ? t.Exception.InnerException
                        : t.Exception);
                    return;
                }

                try
                {
                    tcs.TrySetResult(action());
                }
                catch (OperationCanceledException)
                {
                    tcs.TrySetCanceled();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
            return tcs.Task;
        }
    }
}