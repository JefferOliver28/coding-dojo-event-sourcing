﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using NerdDinner.Events;
using Newtonsoft.Json;

namespace NerdDinner.Models
{
    public class Dinner
    {
        [HiddenInput(DisplayValue = false)]
        public int DinnerID { get; set; }

        [HiddenInput(DisplayValue = false)]
        public Guid DinnerGuid { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(50, ErrorMessage = "Title may not be longer than 50 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Event Date is required")]
        [Display(Name = "Event Date")]
        public DateTime EventDate { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(256, ErrorMessage = "Description may not be longer than 256 characters")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [StringLength(20, ErrorMessage = "Hosted By name may not be longer than 20 characters")]
        [Display(Name = "Host's Name")]
        public string HostedBy { get; set; }

        [Required(ErrorMessage = "Contact info is required")]
        [StringLength(20, ErrorMessage = "Contact info may not be longer than 20 characters")]
        [Display(Name = "Contact Info")]
        public string ContactPhone { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [StringLength(50, ErrorMessage = "Address may not be longer than 50 characters")]
        [Display(Name = "Address, City, State, ZIP")]
        public string Address { get; set; }

        [UIHint("CountryDropDown")]
        public string Country { get; set; }

        [HiddenInput(DisplayValue = false)]
        public double Latitude { get; set; }

        [HiddenInput(DisplayValue = false)]
        public double Longitude { get; set; }

        [HiddenInput(DisplayValue = false)]
        public string HostedById { get; set; }

        private int _currentEvent = 0;

        private List<RSVP> _rsvps = new List<RSVP>();

        [NotMapped]
        public ICollection<RSVP> RSVPs {
            get {
                return _rsvps.AsReadOnly();
            }
        }


        public bool IsHostedBy(string userName)
        {
            return String.Equals(HostedById ?? HostedBy, userName, StringComparison.Ordinal);
        }

        public bool IsUserRegistered(string userName)
        {
            return RSVPs.Any(r => r.AttendeeNameId == userName || (r.AttendeeNameId == null && r.AttendeeName == userName));
        }

        [UIHint("LocationDetail")]
        [NotMapped]
        public LocationDetail Location
        {
            get
            {
                return new LocationDetail() { Latitude = this.Latitude, Longitude = this.Longitude, Title = this.Title, Address = this.Address };
            }
            set
            {
                this.Latitude = value.Latitude;
                this.Longitude = value.Longitude;
                this.Title = value.Title;
                this.Address = value.Address;
            }
        }

        public ICollection<Event> RSVP(string name, string friendlyName) {
            try {
                if (IsUserRegistered(name)) {
                    return new List<Event>();
                }

                var RSVPedEvent = new RSVPed {
                    Name = name,
                    FriendlyName = friendlyName
                };

                RaiseEvent(RSVPedEvent);
                ApplyEvent(RSVPedEvent);

                return this._publishedEvents.ToList();
            }
            finally {
                this._publishedEvents.Clear();
            }
        }

        private readonly List<Event> _publishedEvents = new List<Event>();


        private void RaiseEvent(IEvent eventObject) {
            _publishedEvents.Add(new Event { AggregateId = DinnerGuid, AggregateEventSequence = _currentEvent, DateTime = DateTime.UtcNow, EventType = eventObject.GetType().FullName, Data = JsonConvert.SerializeObject(eventObject) });
            _currentEvent++;
        }

        void ApplyEvent(RSVPed rsvpedEvent) {
            var rsvp = new RSVP();
            rsvp.DinnerID = this.DinnerID;
            rsvp.AttendeeName = rsvpedEvent.FriendlyName;
            rsvp.AttendeeNameId = rsvpedEvent.Name;
            _rsvps.Add(rsvp);
        }

        public void Hydrate(ICollection<Event> events)
        {
            foreach (var e in events.Where(e => e.AggregateEventSequence >= _currentEvent).OrderBy(e => e.AggregateEventSequence))
            { 
                if (e.AggregateEventSequence != _currentEvent) {
                    throw new Exception("Unexpected event sequence");
                }
                var type = Type.GetType(e.EventType);
                dynamic data = JsonConvert.DeserializeObject(e.Data, type);
                ((dynamic)this).ApplyEvent(data);
                _currentEvent++;
            }
        }
    }

    public class LocationDetail
    {
        public double Latitude;
        public double Longitude;
        public string Title;
        public string Address;
    }
}