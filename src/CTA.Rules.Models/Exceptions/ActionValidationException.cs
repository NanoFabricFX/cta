﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CTA.Rules.Models
{
    public class ActionValidationException : Exception
    {
        public ActionValidationException(string actionType, string actionName) 
            : base(GetActionValidationException(actionType, actionName))
        {

        }
        public ActionValidationException(string message) : base(message)
        {
        }

        public static string GetActionValidationException(string actionType, string actionName)
        {
            return string.Format("Error while validation action {0}-{1}", actionType, actionName);
        }
    }
}