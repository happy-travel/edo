﻿using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Mailing;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Notifications.Enums;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace HappyTravel.Edo.Api.NotificationCenter.Services
{
    public interface INotificationService
    {
        Task<Result> Send(ApiCaller apiCaller, JsonDocument message, NotificationTypes notificationType, string email = "", string templateId = "");
        Task<Result> Send(ApiCaller apiCaller, JsonDocument message, NotificationTypes notificationType, List<string> emails, string templateId = "");
        
        Task<Result> Send(int adminId, JsonDocument message, NotificationTypes notificationType, string email = "", string templateId = "");
        Task<Result> Send(int adminId, JsonDocument message, NotificationTypes notificationType, List<string> emails = null, string templateId = "");
        
        Task<Result> Send(SlimAgentContext agent, JsonDocument message, NotificationTypes notificationType, string email = "", string templateId = "");
        Task<Result> Send(SlimAgentContext agent, JsonDocument message, NotificationTypes notificationType, List<string> emails = null, string templateId = "");

        Task<Result> Send(SlimAgentContext agent, DataWithCompanyInfo messageData, NotificationTypes notificationType, List<string> emails = null, string templateId = "");
    }
}
