﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using NhsImsApp.Models;
using NhsImsApp.Data;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using System.Data.Entity;

namespace NhsImsApp.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _Context;

        public AppointmentController()
        {
            _Context = new ApplicationDbContext();
        }

        public AppointmentController(ApplicationDbContext context)
        {
            _Context = context;
        }

        /// <summary>
        ///  Gets appointments grouped by a mounth and count the total appointments in each month
        /// </summary>
        /// <returns>Grouped appointments</returns>
        [HttpPost]
        public ActionResult GetAppointmentsByStaffId(int staffId)
        {
            var now = DateTime.UtcNow;
            var twelveMonthsAgo = now.AddMonths(-12);

            var appointments = _Context.Appointments
                .Where(a => a.StaffId == staffId && a.AppointmentDate >= twelveMonthsAgo && a.AppointmentDate <= now)
                .ToList();

            if (appointments.Count > 0)
            {
                var groupedAppointments = appointments
                    .GroupBy(a => new { a.AppointmentDate.Year, a.AppointmentDate.Month })
                    .OrderBy(g => g.Key.Year)
                    .ThenBy(g => g.Key.Month)
                    .Select(g => new
                    {
                        Month = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(g.Key.Month),
                        Count = g.Count()
                    }).ToList();

                return Json(new
                {
                    success = true,
                    groupedAppointments = groupedAppointments

                });
            }

            return Json(new
            {
                success = false

            });
                
        }

        [HttpPost]
        public ActionResult GetUpcomingAppointmentsByStaffId(int staffId)
        {
            var now = DateTime.UtcNow;

            var appointments = _Context.Appointments
                .Where(a => a.StaffId == staffId && a.AppointmentDate >= now)
                .Include(a => a.Patient) // Include the related Patient data
                .OrderBy(a => a.AppointmentDate) // Order by the appointment date (ascending)
                .Take(10) // Take the top 10 closest appointments
                .ToList();

            if (appointments.Count > 0)
            {
                var upcomingAppointments = appointments.Select(a => new
                {
                    a.AppointmentId,
                    a.Status,
                    a.AppointmentDate,
                    PatientFullName = a.Patient.FullName,
                }).ToList();

                return Json(new
                {
                    success = true,
                    upcomingAppointments = upcomingAppointments
                });
            }

            return Json(new
            {
                success = false
            });
        }

        /// <summary>
        /// Get appointments by selected date range and by staffId
        /// </summary>
        /// <param name="staffId"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns>Matching appointments list</returns>
        [HttpPost]
        public ActionResult GetAppointmentsByRange(int staffId, DateTime startDate, DateTime endDate)
        {
            var appointmentsData = _Context.Appointments
                .Where(a => a.StaffId == staffId && a.AppointmentDate >= startDate && a.AppointmentDate <= endDate)
                .Include(a => a.Patient)
                .OrderBy(a => a.AppointmentDate)
                .ToList();

            if (appointmentsData.Any())
            {
                var appointments = appointmentsData.Select(a => {
                // Make initials from first letter of first and last name
                var names = a.Patient.FullName.Split(' ');
                var initials = "";
                if (names.Length >= 2)
                {
                    initials = names[0].Substring(0, 1) + names[names.Length - 1].Substring(0, 1);
                }
                    return new
                    {
                        a.AppointmentId,
                        a.AppointmentDate,
                        PatientFullName = a.Patient.FullName,
                        PatientDob = a.Patient.Dob,
                        a.AppointmentName,
                        a.Description,
                        a.Status,
                        Initials = initials
                    };
                }).ToList();

                return Json(new
                {
                    success = true,
                    appointments = appointments
                });
            }

            return Json(new
            {
                success = true,
                appointments = 0
            });
        }

        /// <summary>
        /// Add new appointment record
        /// </summary>
        /// <param name="inputModel"></param>
        /// <returns>Status</returns>
        [HttpPost]
        public ActionResult AddAppointmentRecord(AppointmentInputModel inputModel)
        {
            // Create a new Appointment instance
            var newAppointment = new Appointment
            {
                StaffId = inputModel.StaffId,
                PatientId = inputModel.PatientId,
                AppointmentDate = inputModel.AppointmentDate,
                AppointmentName = inputModel.AppointmentName,
                Description = inputModel.Description,
                CreatedTime = DateTime.Now,
                Status = "Scheduled",
                AttendanceConfirmed = false,
            };

            // Add new record to the database
            _Context.Appointments.Add(newAppointment);

            _Context.SaveChanges();

            return Json(new { success = true });
        }

        /// <summary>
        /// Updates appointment details
        /// </summary>
        /// <param name="inputModel"></param>
        /// <returns>Status</returns>
        [HttpPost]
        public ActionResult UpdateAppointment(AppointmentUpdateInputModel inputModel)
        {
            // Find the appointment to update in the database
            var appointmentToUpdate = _Context.Appointments.FirstOrDefault(a => a.AppointmentId == inputModel.AppointmentId);

            // Check if the appointment exists
            if (appointmentToUpdate == null)
            {
                return Json(new { success = false, message = "Appointment not found" });
            }

            // Update the appointment details
            appointmentToUpdate.AppointmentDate = inputModel.AppointmentDate;
            appointmentToUpdate.AppointmentName = inputModel.AppointmentName;
            appointmentToUpdate.Description = inputModel.Description;
            appointmentToUpdate.Status = inputModel.Status;

            // Save the changes to the database
            _Context.SaveChanges();

            return Json(new { success = true });
        }

        /// <summary>
        /// Cancel given appointment
        /// </summary>
        /// <param name="appointmentId"></param>
        /// <returns>Status</returns>
        [HttpPost]
        public ActionResult CancelAppointment(int appointmentId)
        {
            // Find the appointment to update in the database
            var appointmentToUpdate = _Context.Appointments.FirstOrDefault(a => a.AppointmentId == appointmentId);

            // Check if the appointment exists
            if (appointmentToUpdate == null)
            {
                return Json(new { success = false, message = "Appointment not found" });
            }

            // Update the appointment details
            appointmentToUpdate.Status = "Cancelled";

            // Save the changes to the database
            _Context.SaveChanges();

            return Json(new { success = true });
        }

        /// <summary>
        /// Delete given appointment
        /// </summary>
        /// <param name="appointmentId"></param>
        /// <returns>Status</returns>
        [HttpPost]
        public ActionResult DeleteAppointment(int appointmentId)
        {
            // Find the appointment to update in the database
            var appointmentToDelete = _Context.Appointments.FirstOrDefault(a => a.AppointmentId == appointmentId);

            // Check if the appointment exists
            if (appointmentToDelete == null)
            {
                return Json(new { success = false, message = "Appointment not found" });
            }

            // Remove the appointment from the database
            _Context.Appointments.Remove(appointmentToDelete);

            // Save the changes to the database
            _Context.SaveChanges();

            return Json(new { success = true });
        }

    }
}