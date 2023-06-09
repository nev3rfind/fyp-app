﻿using Bogus;
using NhsImsApp.Data;
using NhsImsApp.Models;
using NhsImsApp.Seeders;
using System.Linq;
using System.Data.Entity;

public static class DbInitializer
{
    public static void Initialize(ApplicationDbContext context)
    {
        System.Data.Entity.Database.SetInitializer(new System.Data.Entity.DropCreateDatabaseIfModelChanges<ApplicationDbContext>());

        SeedData(context);
    }

    public static void SeedData(ApplicationDbContext context)
    {
        StaffSeeder.Seed(context);
        PatientSeeder.Seed(context);
        StaffPatientsSeeder.Seed(context);
        AppointmentSeeder.Seed(context);
        ExaminationSeeder.Seed(context);
        PatientExaminations.Seed(context);
        MedicationSeeder.Seed(context);
        PatientMedicationSeeder.Seed(context);
        ProcedureSeeder.Seed(context);
        PatientProcedureSeeder.Seed(context);
    }
}
