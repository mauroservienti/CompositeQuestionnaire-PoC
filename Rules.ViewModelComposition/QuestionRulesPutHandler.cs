﻿using ITOps.Json;
using ITOps.ViewModelComposition;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using NServiceBus;
using Rules.Messages;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Rules.ViewModelComposition
{
    public class QuestionRulesPutHandler : IHandleRequests, IHandleRequestsErrors
    {
        IMessageSession messageSession;

        public QuestionRulesPutHandler(IMessageSession messageSession)
        {
            this.messageSession = messageSession;
        }

        public bool Matches(RouteData routeData, string httpVerb, HttpRequest request)
        {
            var controller = (string)routeData.Values["controller"];
            var action = (string)routeData.Values["action"];

            return HttpMethods.IsPut(httpVerb)
                   && controller.ToLowerInvariant() == "questions"
                   && string.IsNullOrWhiteSpace(action)
                   && !routeData.Values.ContainsKey("id");
        }

        public async Task Handle(string requestId, dynamic vm, RouteData routeData, HttpRequest request)
        {
            dynamic putData = request.Body.AsExpando();
            var rules = new List<dynamic>()
            {
                new
                {
                    putData.QuestionId,
                    putData.CorrectAnswerId
                }
            };

            var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"http://localhost:54219/api/rules")
            {
                Content = new StringContent(JsonConvert.SerializeObject(rules), Encoding.UTF8, "application/json")
            };
            requestMessage.Headers.AddComposedRequestIdHeader(requestId);

            var response = await new HttpClient()
                .SendAsync(requestMessage);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(response.ReasonPhrase);
            }
        }

        public Task OnRequestError(string requestId, Exception ex, dynamic vm, RouteData routeData, HttpRequest request)
        {
            return messageSession.Send("Rules.Api", new CleanupFailedRequest()
            {
                RequestId = requestId
            });
        }
    }
}
