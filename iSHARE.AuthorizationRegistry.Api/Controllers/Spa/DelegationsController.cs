﻿using System;
using System.Linq;
using System.Threading.Tasks;
using iSHARE.Abstractions;
using iSHARE.AuthorizationRegistry.Api.ViewModels;
using iSHARE.AuthorizationRegistry.Core.Api;
using iSHARE.AuthorizationRegistry.Core.Requests;
using iSHARE.Identity.Api.Controllers;
using iSHARE.IdentityServer.Delegation;
using iSHARE.Models;
using iSHARE.Models.DelegationMask;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iSHARE.AuthorizationRegistry.Api.Controllers.Spa
{
    [Route("delegations")]
    public class DelegationsController : SpaAuthorizedController
    {
        private readonly IDelegationService _delegationService;
        private readonly IDelegationValidationService _delegationValidationService;
        private readonly IDelegationTranslateService _delegationTranslateService;
        private readonly IDelegationMaskValidationService _delegationMaskValidationService;

        public DelegationsController(
            IDelegationService delegationService,
            IDelegationValidationService delegationValidationService,
            IDelegationTranslateService delegationTranslateService,
            IDelegationMaskValidationService delegationMaskValidationService)
        {
            _delegationService = delegationService;
            _delegationValidationService = delegationValidationService;
            _delegationTranslateService = delegationTranslateService;
            _delegationMaskValidationService = delegationMaskValidationService;
        }

        [HttpGet]
        [Authorize(Roles = Constants.Roles.SchemeOwner + "," +
                           Constants.Roles.AuthorizationRegistry.PartyAdmin + "," +
                           Constants.Roles.AuthorizationRegistry.EntitledPartyViewer + "," +
                           Constants.Roles.AuthorizationRegistry.EntitledPartyCreator)]
        public async Task<IActionResult> GetAll([FromQuery]DelegationQuery query)
        {
            query.PartyId = User.GetPartyId();

            var delegations = await _delegationService.GetAll(query);

            return Ok(new PagedResult<DelegationOverviewViewModel>
            {
                Data = delegations.Data.Select(d => d.MapToOverviewViewModel()),
                Count = delegations.Count
            });
        }

        [HttpGet]
        [Route("{arId}", Name = "GetDelegation")]
        [Authorize(Roles = Constants.Roles.SchemeOwner + "," +
                           Constants.Roles.AuthorizationRegistry.PartyAdmin + "," +
                           Constants.Roles.AuthorizationRegistry.EntitledPartyViewer + "," +
                           Constants.Roles.AuthorizationRegistry.EntitledPartyCreator)]
        public async Task<IActionResult> GetByAuthorizationRegistryId(string arId)
        {
            var delegation = await _delegationService.GetByPolicyId(arId, User.GetPartyId());
            return OkOrNotFound(delegation?.MapToViewModel());
        }

        [HttpPost, Authorize(Roles = Constants.Roles.AuthorizationRegistry.EntitledPartyCreator)]
        public async Task<IActionResult> Create([FromBody]CreateOrUpdateDelegationRequest request)
        {
            request.UserId = User.GetUserId();
            request.PartyId = User.GetPartyId();
            try
            {
                var validationResult = request.IsCopy ? _delegationValidationService.ValidateCopy(request.Policy, User)
                                                  : await _delegationValidationService.ValidateCreate(request.Policy, User);
                if (!validationResult.Success)
                {
                    return BadRequest(validationResult.Error);
                }
            }
            catch (DelegationPolicyFormatException ex)
            {
                return BadRequest(ex.Message);
            }

            var delegation = request.IsCopy ? await _delegationService.Copy(request) : await _delegationService.Create(request);

            return CreatedAtRoute("GetDelegation", new { arId = delegation.AuthorizationRegistryId }, delegation.MapToViewModel());
        }

        [HttpPut, Route("{arId}"), Authorize(Roles = Constants.Roles.AuthorizationRegistry.EntitledPartyCreator)]
        public async Task<IActionResult> Edit(string arId, [FromBody]CreateOrUpdateDelegationRequest request)
        {
            request.UserId = User.GetUserId();
            request.PartyId = User.GetPartyId();

            try
            {
                var validationResult = await _delegationValidationService.ValidateEdit(arId, request.Policy, User);
                if (!validationResult.Success)
                {
                    return BadRequest(validationResult.Error);
                }
            }
            catch (DelegationPolicyFormatException ex)
            {
                return BadRequest(ex.Message);
            }

            var delegation = await _delegationService.Update(arId, request);

            return Ok(delegation.MapToViewModel());
        }

        [HttpDelete, Route("{arId}"), Authorize(Roles = Constants.Roles.AuthorizationRegistry.EntitledPartyCreator)]
        public async Task<IActionResult> Delete(string arId)
        {
            await _delegationService.MakeInactive(arId, User.GetUserId());
            return Ok();
        }

        [HttpGet, Route("json/{id:guid}")]
        [Authorize(Roles = Constants.Roles.SchemeOwner + "," +
                           Constants.Roles.AuthorizationRegistry.PartyAdmin + "," +
                           Constants.Roles.AuthorizationRegistry.EntitledPartyViewer + "," +
                           Constants.Roles.AuthorizationRegistry.EntitledPartyCreator)]
        public async Task<IActionResult> GetDelegationJson(Guid id)
        {
            var delegation = await _delegationService.Get(id, User.GetPartyId());

            if (delegation == null)
            {
                return NotFound();
            }

            var fileName = $"{delegation.CreatedDate:yy-MM-dd}-{delegation.AuthorizationRegistryId}.json";
            return BuildJsonDownloadFileResult(fileName, delegation.Policy);
        }

        [HttpGet, Route("history/json/{id:guid}")]
        [Authorize(Roles = Constants.Roles.SchemeOwner + "," +
                           Constants.Roles.AuthorizationRegistry.PartyAdmin + "," +
                           Constants.Roles.AuthorizationRegistry.EntitledPartyViewer + "," +
                           Constants.Roles.AuthorizationRegistry.EntitledPartyCreator)]
        public async Task<IActionResult> GetDelegationHistoryJson(Guid id)
        {
            var query = new DelegationHistoryQuery
            {
                Id = id,
                PartyId = User.GetPartyId()
            };

            var delegationHistory = await _delegationService.GetDelegationHistoryById(query);
            if (delegationHistory == null)
            {
                return NotFound();
            }

            var fileName = $"History-{delegationHistory.CreatedDate:yy-MM-dd}-{delegationHistory.Delegation?.AuthorizationRegistryId}.json";
            return BuildJsonDownloadFileResult(fileName, delegationHistory.Policy);
        }

        [HttpPost, Route("test")]
        [Authorize(Roles = Constants.Roles.SchemeOwner + "," +
                           Constants.Roles.AuthorizationRegistry.PartyAdmin + "," +
                           Constants.Roles.AuthorizationRegistry.EntitledPartyViewer + "," +
                           Constants.Roles.AuthorizationRegistry.EntitledPartyCreator)]
        public async Task<ActionResult<DelegationTranslationTestResponse>> TestDelegationTranslation([FromBody]DelegationMask delegationMask)
        {
            var validationResult = _delegationMaskValidationService.Validate(delegationMask);
            if (!validationResult.Success)
            {
                return BadRequest(new { error = validationResult.Error });
            }
            var delegation = await _delegationService
                .GetBySubject(delegationMask.DelegationRequest.Target.AccessSubject, delegationMask.DelegationRequest.PolicyIssuer)
                .ConfigureAwait(false);
            if (delegation == null)
            {
                return NotFound();
            }
            var delegationResponse = _delegationTranslateService.Translate(delegationMask, delegation.Policy);
            return delegationResponse;
        }
    }
}
