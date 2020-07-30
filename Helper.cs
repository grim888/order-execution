using System;
using System.Collections.Generic;

namespace blockshift_ib
{
    public class DateToday
    {
        public static string Today { get; set; }
    }
    public class Precision
    {
        public static decimal UltimateRoundingFunction(decimal amountToRound, decimal nearstOf, decimal fairness)
        {
            //amountToRound => input amount
            //nearestOf => .25 if round to quater, 0.01 for rounding to 1 cent, 1 for rounding to $1
            //fairness => btween 0 to 0.9999999___.
            //            0 means floor and 0.99999... means ceiling. But for ceiling, I would recommend, Math.Ceiling
            //            0.5 = Standard Rounding function. It will round up the border case. i.e. 1.5 to 2 and not 1.
            //            0.4999999... non-standard rounding function. Where border case is rounded down. i.e. 1.5 to 1 and not 2.
            //            0.75 means first 75% values will be rounded down, rest 25% value will be rounded up.
            return Math.Floor(amountToRound / nearstOf + fairness) * nearstOf;
        }
    }

    public class Slack
    {
        private readonly Parameters @params;
        public Slack()
        {
        }
        public Slack(Parameters @params)
        {
            this.@params = @params;
        }
        public void SendMessageToSlack(string slackMsg)
        {
            string urlWithAccessToken = "https://hooks.slack.com/services/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";
            SlackClient client = new SlackClient(urlWithAccessToken);

            client.PostMessage(username: "orderApp", text: slackMsg, channel: "#testing");
        }
        public void SendMessageToSlack(string slackMsg, Parameters @params)
        {
            if (@params.sendSlackMessage)
            {
                string urlWithAccessToken = "https://hooks.slack.com/services/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";
                SlackClient client = new SlackClient(urlWithAccessToken);

                client.PostMessage(username: "orderApp", text: slackMsg, channel: "#testing");
            }
        }
    }
}