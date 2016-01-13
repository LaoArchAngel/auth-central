﻿using System.Linq;
using System.Threading.Tasks;
using Fsw.Enterprise.AuthCentral.Areas.Admin.Models;
using Fsw.Enterprise.AuthCentral.MongoStore.Admin;
using IdentityServer3.Core.Models;
using Microsoft.AspNet.Mvc;

namespace Fsw.Enterprise.AuthCentral.Areas.Admin.Controllers
{
    [Area("Admin"), Route("[area]/[controller]")]
    public class ScopeController : Controller
    {
        private readonly IScopeService _scopeService;

        public ScopeController(IScopeService scopeService)
        {
            _scopeService = scopeService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new ScopeListModel(_scopeService.Get().Result);
            return View(model);
        }

        [HttpPost("[action]")]
        public IActionResult Edit()
        {
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Removes the given claim from the given scope.
        /// </summary>
        /// <param name="scope">Unique identifier for the scope.</param>
        /// <param name="claim">Unique identifier for the claim.</param>
        /// <returns></returns>
        [HttpPost("[action]")]
        public async Task<IActionResult> RemoveClaim(string scope, string claim)
        {
            var editScope = await _scopeService.Find(scope);

            if (editScope == null)
            {
                ModelState.AddModelError("FindScope",$"Scope with the name {scope} could not be found.");
                return RedirectToAction("Index");
            }

            var claimToRemove = editScope.Claims.SingleOrDefault(scopeClaim => scopeClaim.Name == claim);

            if (claimToRemove == default(ScopeClaim))
            {
                ModelState.AddModelError("FindClaim", $"Claim with the name {claim} was not found in the scope {scope}");
                return RedirectToAction("Index");
            }

            editScope.Claims.Remove(claimToRemove);
            await _scopeService.Save(editScope);

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Changes an existing claim in a scope.
        /// </summary>
        /// <param name="claim">The <see cref="ScopeClaim"/> with all its (possibly changed) values.</param>
        /// <param name="claimId">The original name of the claim.  Might be different in <paramref name="claim"/></param>
        /// <param name="scope">The name of the scope we're updating claims in.</param>
        [HttpPost("[action]")]
        public async Task<IActionResult> EditClaim(ScopeClaim claim, string claimId, string scope)
        {
            var claimScope = await _scopeService.Find(scope);

            if (claimScope == null)
            {
                ModelState.AddModelError("FindScope", $"Scope with the name {scope} could not be found.");
                return RedirectToAction("Index");
            }

            var claimToRemove = claimScope.Claims.SingleOrDefault(scopeClaim => scopeClaim.Name == claimId);

            if (claimToRemove == default(ScopeClaim))
            {
                ModelState.AddModelError("FindClaim", $"Claim with the name {claimId} was not found in the scope {scope}");
                return RedirectToAction("Index");
            }

            int index = claimScope.Claims.IndexOf(claimToRemove);
            claimScope.Claims[index] = claim;
            
            await _scopeService.Save(claimScope);

            return RedirectToAction("Index");
        }
    }
}