using System;
using System.Web;
using System.Web.Mvc;
using System.Collections.Generic;
using System.Linq;
using NerdDinner.Helpers;
using NerdDinner.Models;
using DDay.iCal;
using DDay.iCal.Components;
using DDay.iCal.Serialization;

namespace NerdDinner.Controllers {

    [HandleErrorWithELMAH]
    public class DinnersController : Controller {

        IDinnerRepository dinnerRepository;

        //
        // Dependency Injection enabled constructors

        public DinnersController()
            : this(new DinnerRepository()) {
        }

        public DinnersController(IDinnerRepository repository) {
            dinnerRepository = repository;
        }

        //
        // GET: /Dinners/
        //      /Dinners/Page/2
        //      /Dinners?q=term

        public ActionResult Index(string q, int? page) {

            const int pageSize = 25;

            IQueryable<Dinner> dinners = null;

            //Searching?
            if (!string.IsNullOrWhiteSpace(q))
                dinners = new DinnerRepository().FindDinnersByText(q);
            else 
                dinners = dinnerRepository.FindUpcomingDinners();

            var paginatedDinners = new PaginatedList<Dinner>(dinners, page ?? 0, pageSize);

            return View(paginatedDinners);
        }

        //
        // GET: /Dinners/Details/5

        public ActionResult Details(int? id) {
            if (id == null) {
                return new FileNotFoundResult { Message = "No Dinner found due to invalid dinner id" };
            }

            Dinner dinner = dinnerRepository.GetDinner(id.Value);

            if (dinner == null) {
                return new FileNotFoundResult { Message = "No Dinner found for that id" };
            }

            return View(dinner);
        }

        //
        // GET: /Dinners/Edit/5

        [Authorize]
        public ActionResult Edit(int id) {

            Dinner dinner = dinnerRepository.GetDinner(id);

            if (!dinner.IsHostedBy(User.Identity.Name))
                return View("InvalidOwner");

            return View(new DinnerFormViewModel(dinner));
        }

        //
        // POST: /Dinners/Edit/5

        [HttpPost, Authorize]
        public ActionResult Edit(int id, FormCollection collection) {

            Dinner dinner = dinnerRepository.GetDinner(id);

            if (!dinner.IsHostedBy(User.Identity.Name))
                return View("InvalidOwner");

            try {
                UpdateModel(dinner);

                dinnerRepository.Save();

                return RedirectToAction("Details", new { id=dinner.DinnerID });
            }
            catch {
                return View(new DinnerFormViewModel(dinner));
            }
        }

        //
        // GET: /Dinners/Create

        [Authorize]
        public ActionResult Create() {

            Dinner dinner = new Dinner() {
               EventDate = DateTime.Now.AddDays(7)
            };

            return View(new DinnerFormViewModel(dinner));
        } 

        //
        // POST: /Dinners/Create

        [HttpPost, Authorize]
        public ActionResult Create(Dinner dinner) {

            if (ModelState.IsValid) {

                try {
                    NerdIdentity nerd = (NerdIdentity)User.Identity;
                    dinner.HostedById = nerd.Name;
                    dinner.HostedBy = nerd.FriendlyName;

                    RSVP rsvp = new RSVP();
                    rsvp.AttendeeNameId = nerd.Name;
                    rsvp.AttendeeName = nerd.FriendlyName;
                    dinner.RSVPs.Add(rsvp);

                    dinnerRepository.Add(dinner);
                    dinnerRepository.Save();

                    return RedirectToAction("Details", new { id=dinner.DinnerID });
                }
                catch {
                }
            }

            return View(new DinnerFormViewModel(dinner));
        }

        //
        // HTTP GET: /Dinners/Delete/1

        [Authorize]
        public ActionResult Delete(int id) {

            Dinner dinner = dinnerRepository.GetDinner(id);

            if (dinner == null)
                return View("NotFound");

            if (!dinner.IsHostedBy(User.Identity.Name))
                return View("InvalidOwner");

            return View(dinner);
        }

        // 
        // HTTP POST: /Dinners/Delete/1

        [HttpPost, Authorize]
        public ActionResult Delete(int id, string confirmButton) {

            Dinner dinner = dinnerRepository.GetDinner(id);

            if (dinner == null)
                return View("NotFound");

            if (!dinner.IsHostedBy(User.Identity.Name))
                return View("InvalidOwner");

            dinnerRepository.Delete(dinner);
            dinnerRepository.Save();

            return View("Deleted");
        }

  
        protected override void HandleUnknownAction(string actionName)
        {
            throw new HttpException(404, "Action not found");
        }

        public ActionResult Confused()
        {
            return View();
        }

        public ActionResult Trouble()
        {
            return View("Error");
        }
    }
}