﻿using Dynamitey;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StatefulPatternFunctions.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace StatefulPatternFunctions.CertificationManager
{
    public class CertificationProfileManagement
    {
        [FunctionName("InitializeCertificationProfile")]
        public async Task<IActionResult> InitializeProfile(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "profiles")] HttpRequest req,
            [DurableClient] IDurableEntityClient client,
            ILogger logger)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var profile = JsonConvert.DeserializeObject<CertificationProfileInitializeModel>(requestBody);

            var entityId = new EntityId(nameof(CertificationProfileEntity), profile.Id.ToString());

            await client.SignalEntityAsync(entityId,
                   nameof(CertificationProfileEntity.InitializeProfile), profile);

            return new OkObjectResult($"Profile {profile.Id} initialized");
        }

        [FunctionName("UpdateCertificationProfile")]
        public async Task<IActionResult> UpdateProfile(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "profiles/{profileId}")] HttpRequest req,
            Guid profileId,
            [DurableClient] IDurableEntityClient client,
            ILogger logger)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var profile = JsonConvert.DeserializeObject<CertificationProfileUpdateModel>(requestBody);

            var entityId = new EntityId(nameof(CertificationProfileEntity), profileId.ToString());

            await client.SignalEntityAsync(entityId,
                   nameof(CertificationProfileEntity.UpdateProfile), profile);

            return new OkObjectResult($"Profile {profileId} updated");
        }

        [FunctionName("DeleteCertificationProfile")]
        public async Task<IActionResult> DeleteProfile(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "profiles/{profileId}")] HttpRequest req,
            Guid profileId,
            [DurableClient] IDurableEntityClient client,
            ILogger logger)
        {
            var entityId = new EntityId(nameof(CertificationProfileEntity), profileId.ToString());

            await client.SignalEntityAsync(entityId,
                   nameof(CertificationProfileEntity.DeleteProfile), null);

            return new OkObjectResult($"Profile {profileId} updated");
        }

        [FunctionName("AddCertification")]
        public async Task<IActionResult> AddCertification(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "profiles/{profileId}/certifications")] HttpRequest req,
            Guid profileId,
            [DurableClient] IDurableEntityClient client,
            ILogger logger)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var certification = JsonConvert.DeserializeObject<CertificationUpsertModel>(requestBody);

            var entityId = new EntityId(nameof(CertificationProfileEntity), profileId.ToString());

            await client.SignalEntityAsync(entityId,
                   nameof(CertificationProfileEntity.UpsertCertification), certification);

            return new OkObjectResult($"Certification {certification.Id} added to profile {profileId}");
        }

        [FunctionName("UpdateCertification")]
        public async Task<IActionResult> UpdateCertification(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "profiles/{profileId}/certifications/{certificationId}")] HttpRequest req,
            Guid profileId,
            Guid certificationId,
            [DurableClient] IDurableEntityClient client,
            ILogger logger)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var certification = JsonConvert.DeserializeObject<CertificationUpsertModel>(requestBody);
            certification.Id = certificationId;

            var entityId = new EntityId(nameof(CertificationProfileEntity), profileId.ToString());

            await client.SignalEntityAsync(entityId,
                   nameof(CertificationProfileEntity.UpsertCertification), certification);

            return new OkObjectResult($"Certification {certification.Id} update for profile {profileId}");
        }

        [FunctionName("GetCertificationProfiles")]
        public static async Task<IActionResult> GetProfiles(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "profiles")] HttpRequest req,
            [DurableClient] IDurableEntityClient client)
        {
            var result = new List<CertificationProfilesGetModel>();

            var query = new EntityQuery()
            {
                PageSize = 100,
                FetchState = true
            };

            do
            {
                var profiles = await client.ListEntitiesAsync(query, default);

                foreach (var profile in profiles.Entities)
                {
                    var profileModel = profile.State.ToObject<CertificationProfilesGetModel>();
                    if (!profileModel.IsDeleted)
                    {
                        profileModel.Id = Guid.Parse(profile.EntityId.EntityKey);
                        result.Add(profileModel);
                    }
                }

                query.ContinuationToken = profiles.ContinuationToken;
            } while (query.ContinuationToken != null && query.ContinuationToken != "bnVsbA==");

            return new OkObjectResult(result);
        }

        [FunctionName("GetCertificationProfile")]
        public static async Task<IActionResult> GetProfile(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "profiles/{profileId}")] HttpRequest req,
            string profileId,
            [DurableClient] IDurableEntityClient client)
        {
            var entityId = new EntityId(nameof(CertificationProfileEntity), profileId);

            var entity = await client.ReadEntityStateAsync<JObject>(entityId);
            if (entity.EntityExists)
            {
                var profile = entity.EntityState.ToObject<CertificationProfileGetModel>();
                if (!profile.IsDeleted)
                {
                    profile.Id = Guid.Parse(profileId);
                    return new OkObjectResult(profile);
                }
            }
            return new NotFoundObjectResult(profileId);
        }
    }
}
