﻿using System.Linq;
using System.Threading.Tasks;
using Fsw.Enterprise.AuthCentral.Areas.Admin.Models;
using Fsw.Enterprise.AuthCentral.MongoStore.Admin;
using IdentityServer3.Core.Models;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;

namespace Fsw.Enterprise.AuthCentral.Areas.Admin.Controllers
{
    /// <summary>
    /// Controller to create, delete and edit everything about scopes.
    /// </summary>
    [Authorize("FswAdmin")]
    [Area("Admin"), Route("[area]/[controller]")]
    public class ScopeController : Controller
    {
        private readonly IScopeService _scopeService;

        /// <summary>
        /// Creates a new instance of <see cref="ScopeController"/>.
        /// </summary>
        /// <param name="scopeService">The service used to find and edit <see cref="Scope"/> information.</param>
        public ScopeController(IScopeService scopeService)
        {
            _scopeService = scopeService;
        }

        /// <summary>
        /// Primary action for the scope controller.  Displays a list of scopes.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new ScopeListModel(await _scopeService.Get(false));
            return View(model);
        }

        /// <summary>
        ///     Creates a scope from user input
        /// </summary>
        /// <param name="newScope">Model containing user's scope details.</param>
        [HttpPost("[action]"), ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateScope(ScopeModel newScope)
        {
            Scope dupe = await _scopeService.Find(newScope.Name);

            if (dupe != null)
            {
                ModelState.AddModelError("FindScope", $"A scope with name {newScope.Name} already exists.");
                return RedirectToAction("Index");
            }

            await _scopeService.Save(newScope.IdsScope);

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Adds a new claim to a <paramref name="scope">specified scope</paramref>.
        /// </summary>
        /// <remarks>Fails if the scope does not exist; or if the scope already contains a claim with the given name.</remarks>
        /// <param name="scope">Name of the scope to which we intend to add a claim.</param>
        /// <param name="claim">Claim to add.</param>
        [HttpPost("[action]"), ValidateAntiForgeryToken]
        public async Task<IActionResult> AddClaim(string scope, string claim)
        {
            var claimScope = await _scopeService.Find(scope);

            if (claimScope == null)
            {
                ModelState.AddModelError("AddClaim", $"Scope with the name {scope} could not be found");
                return PartialView("ScopeClaimList");
            }

            if (claimScope.Claims.Any(scopeClaim => scopeClaim.Name == claim))
            {
                ModelState.AddModelError("AddClaim", $"Claim with name {claim} already exists for scope {scope}");
                return PartialView("ScopeClaimList", new ScopeModel(claimScope));
            }

            var newClaim = new ScopeClaim(claim);

            claimScope.Claims.Add(newClaim);

            await _scopeService.Save(claimScope);

            return PartialView("ScopeClaimList", new ScopeModel(claimScope));
        }

        /// <summary>
        /// Edit an existing scope.
        /// </summary>
        /// <remarks>Fails if the <paramref name="scopeName">specified scope</paramref> doesn't exist</remarks>
        /// <param name="scopeName">Original name of the scope.</param>
        /// <param name="scope"></param>
        /// <returns></returns>
        [HttpPost("[action]"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string scopeName, ScopeModel scope)
        {
            var editScope = await _scopeService.Find(scopeName);

            if (editScope == null)
            {
                ModelState.AddModelError("FindScope", $"Scope with the name {scopeName} could not be found.");
                return RedirectToAction("Index");
            }

            await _scopeService.Delete(scopeName);

            scope.IdsScope.Claims = editScope.Claims;

            await _scopeService.Save(scope.IdsScope);

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Removes the given claim from the given scope.
        /// </summary>
        /// <param name="scope">Unique identifier for the scope.</param>
        /// <param name="claim">Unique identifier for the claim.</param>
        /// <returns></returns>
        [HttpPost("[action]"), ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveClaim(string scope, string claim)
        {
            var editScope = await _scopeService.Find(scope);

            if (editScope == null)
            {
                ModelState.AddModelError("FindScope",$"Scope with the name {scope} could not be found.");
                return PartialView("ScopeClaimList");
            }

            var claimToRemove = editScope.Claims.SingleOrDefault(scopeClaim => scopeClaim.Name == claim);

            if (claimToRemove == default(ScopeClaim))
            {
                ModelState.AddModelError("FindClaim", $"Claim with the name {claim} was not found in the scope {scope}");
                return PartialView("ScopeClaimList");
            }

            editScope.Claims.Remove(claimToRemove);
            await _scopeService.Save(editScope);
            var scopeModel = new ScopeModel(editScope);

            return PartialView("ScopeClaimList", scopeModel);
        }

        /// <summary>
        /// Deletes the given scope from the store.
        /// </summary>
        /// <param name="scopeName">Unique name / ID of the scope</param>
        [HttpPost("[action]"), ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveScope(string scopeName)
        {
            var scope = await _scopeService.Find(scopeName);

            if (scope == null)
                return RedirectToAction("Index");

            await _scopeService.Delete(scopeName);

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Changes an existing claim in a scope.
        /// </summary>
        /// <param name="claim">The <see cref="ScopeClaim"/> with all its (possibly changed) values.</param>
        /// <param name="claimId">The original name of the claim.  Might be different in <paramref name="claim"/></param>
        /// <param name="scope">The name of the scope we're updating claims in.</param>
        [HttpPost("[action]"), ValidateAntiForgeryToken]
        public async Task<IActionResult> EditClaim(ScopeClaimModel claimModel)
        {
            var claimScope = await _scopeService.Find(claimModel.ScopeName);

            if (claimScope == null)
            {
                ModelState.AddModelError("FindScope", $"Scope with the name {claimModel.ScopeName} could not be found.");
                return PartialView("ScopeClaimList");
            }

            var claimToRemove = claimScope.Claims.SingleOrDefault(scopeClaim => scopeClaim.Name == claimModel.ClaimId);

            if (claimToRemove == default(ScopeClaim))
            {
                ModelState.AddModelError("FindClaim", $"Claim with the name {claimModel.ClaimId} was not found in the scope {claimModel.ScopeName}");
                return PartialView("ScopeClaimList");
            }

            int index = claimScope.Claims.IndexOf(claimToRemove);
            claimScope.Claims[index] = claimModel.ScopeClaim;
            
            await _scopeService.Save(claimScope);
            
            var scopeModel = new ScopeModel(claimScope);

            ModelState.Clear();
            PartialViewResult partialViewResult = PartialView("ScopeClaimList", scopeModel);
            return partialViewResult;
        }
    }
}