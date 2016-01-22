using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Rtc.Collaboration;
using Microsoft.Rtc.Signaling;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class InstantMessagingService : IInstantMessagingService
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string _username;
        private readonly string _password;

        public InstantMessagingService(string username, string password)
        {
            _username = username;
            _password = password;
        }

        public void SendMessage(string[] targets, string subject, string message)
        {
            var cps = new ClientPlatformSettings(null, SipTransportType.Tls);
            var cp = new CollaborationPlatform(cps);
            var us = new UserEndpointSettings(string.Format("sip:{0}", _username))
            {
                Credential = new NetworkCredential(_username, _password)
            };
            log.DebugFormat("OwnerUri: {0}", us.OwnerUri);
            var ue = new UserEndpoint(cp, us);

            // start up the platform
            Task.Factory.FromAsync(ue.Platform.BeginStartup, ue.Platform.EndStartup, null).Wait();
            Task.Factory.FromAsync<SipResponseData>(ue.BeginEstablish, ue.EndEstablish, null).Wait();

            // send the messages
            var tasks = targets.Select(t =>
            {
                var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                var tsc = new TaskCompletionSource<SendInstantMessageResult>();
                cts.Token.Register(() => tsc.TrySetCanceled());

                var sent = false;
                var doSend = new Action<InstantMessagingFlow>(flow =>
                {
                    if (sent || flow.State != MediaFlowState.Active)
                    {
                        return;
                    }
                    sent = true;
                    Task.Factory.FromAsync<string, SendInstantMessageResult>(
                        flow.BeginSendInstantMessage,
                        flow.EndSendInstantMessage,
                        message,
                        null)
                        .ContinueWith(
                            r =>
                            {
                                if (r.IsCanceled)
                                {
                                    tsc.TrySetCanceled();
                                }
                                else if (r.IsFaulted)
                                {
                                    tsc.TrySetException(r.Exception);
                                }
                                else
                                {
                                    tsc.TrySetResult(r.Result);
                                }
                            });
                });

                var c = new Conversation(ue, new ConversationSettings() {Priority = ConversationPriority.Urgent});
                var im = new InstantMessagingCall(c);
                im.InstantMessagingFlowConfigurationRequested += (sender, args) =>
                {
                    doSend(im.Flow);
                    im.Flow.StateChanged += (o, eventArgs) =>
                    {
                        doSend(im.Flow);
                    };
                };
                Task.Factory.FromAsync<string, ToastMessage, CallEstablishOptions, CallMessageData>(im.BeginEstablish,
                    im.EndEstablish, "sip:" + t, new ToastMessage(subject), null, null).ContinueWith(r =>
                    {
                        if (r.IsCanceled)
                        {
                            tsc.TrySetCanceled();
                        }
                        else if (r.IsFaulted)
                        {
                            tsc.TrySetException(r.Exception);
                        }
                    });
                return tsc.Task;
            }).ToArray();

            // wait for the messages to go
            Task.WhenAll(tasks).ContinueWith(r =>
            {
                if (r.IsCanceled)
                {
                    log.DebugFormat("Message send was cancelled");
                }
                else if (r.IsFaulted)
                {
                    log.WarnFormat("Message send failed: {0}", r.Exception);
                }
                Task.Factory.FromAsync(ue.Platform.BeginShutdown, ue.Platform.EndShutdown, null).Wait();
            });
        }
    }
}