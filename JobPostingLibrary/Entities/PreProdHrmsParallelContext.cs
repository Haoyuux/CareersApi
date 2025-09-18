using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace JobPostingLibrary.Entities;

public partial class PreProdHrmsParallelContext : DbContext
{
    public PreProdHrmsParallelContext()
    {
    }

    public PreProdHrmsParallelContext(DbContextOptions<PreProdHrmsParallelContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AppBinaryObject> AppBinaryObjects { get; set; }

    public virtual DbSet<Contract> Contracts { get; set; }

    public virtual DbSet<Hr201allowance> Hr201allowances { get; set; }

    public virtual DbSet<Hr201allowanceType> Hr201allowanceTypes { get; set; }

    public virtual DbSet<Hr201appointment> Hr201appointments { get; set; }

    public virtual DbSet<Hr201businessUnit> Hr201businessUnits { get; set; }

    public virtual DbSet<Hr201civilStatus> Hr201civilStatuses { get; set; }

    public virtual DbSet<Hr201employee> Hr201employees { get; set; }

    public virtual DbSet<Hr201gender> Hr201genders { get; set; }

    public virtual DbSet<Hr201location> Hr201locations { get; set; }

    public virtual DbSet<Mrdetail> Mrdetails { get; set; }

    public virtual DbSet<PlantillaJobMngmnt> PlantillaJobMngmnts { get; set; }

    public virtual DbSet<PlantillaJobTitle> PlantillaJobTitles { get; set; }

    public virtual DbSet<PlantillaRank> PlantillaRanks { get; set; }

    public virtual DbSet<RecrtmntApplcntEducDetail> RecrtmntApplcntEducDetails { get; set; }

    public virtual DbSet<RecrtmntApplcntEducHeader> RecrtmntApplcntEducHeaders { get; set; }

    public virtual DbSet<RecrtmntApplicantMasterlist> RecrtmntApplicantMasterlists { get; set; }

    public virtual DbSet<RecrtmntExperienceDetail> RecrtmntExperienceDetails { get; set; }

    public virtual DbSet<RecrtmntExperienceHeader> RecrtmntExperienceHeaders { get; set; }

    public virtual DbSet<RecrtmntJobAdvertisement> RecrtmntJobAdvertisements { get; set; }

    public virtual DbSet<RecrtmntJobPostingDetail> RecrtmntJobPostingDetails { get; set; }

    public virtual DbSet<RecrtmntJobPostingDetailAuditLog> RecrtmntJobPostingDetailAuditLogs { get; set; }

    public virtual DbSet<RecrtmntJobPostingHeader> RecrtmntJobPostingHeaders { get; set; }

    public virtual DbSet<RecrtmntJobPostingReqChecklist> RecrtmntJobPostingReqChecklists { get; set; }

    public virtual DbSet<RecrtmntJobPostingheaderAuditLog> RecrtmntJobPostingheaderAuditLogs { get; set; }

    public virtual DbSet<RecrtmntRequirementChecklist> RecrtmntRequirementChecklists { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("HrmsConnection");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppBinaryObject>(entity =>
        {
            entity.HasIndex(e => e.TenantId, "IX_AppBinaryObjects_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasIndex(e => e.EmploymentStatId, "IX_Contracts_EmploymentStatId");

            entity.HasIndex(e => e.Hr201designationId, "IX_Contracts_HR201DesignationId");

            entity.HasIndex(e => e.JobOfferFromId, "IX_Contracts_JobOfferFromId");

            entity.HasIndex(e => e.LetterFrom, "IX_Contracts_LetterFrom");

            entity.HasIndex(e => e.PlantillaJobTitleId, "IX_Contracts_PlantillaJobTitleId");

            entity.HasIndex(e => e.RecrtmntJobPostDetailId, "IX_Contracts_RecrtmntJobPostDetailId");

            entity.HasIndex(e => e.TenantId, "IX_Contracts_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ApprovedByGmtime).HasColumnName("ApprovedByGMTime");
            entity.Property(e => e.ApproverBu).HasColumnName("ApproverBU");
            entity.Property(e => e.Hr201designationId).HasColumnName("HR201DesignationId");
            entity.Property(e => e.IsApprovedByGm).HasColumnName("IsApprovedByGM");
            entity.Property(e => e.JobOfferPdf).HasColumnName("JobOfferPDF");
            entity.Property(e => e.Salary).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.JobOfferFrom).WithMany(p => p.ContractJobOfferFroms).HasForeignKey(d => d.JobOfferFromId);

            entity.HasOne(d => d.LetterFromNavigation).WithMany(p => p.ContractLetterFromNavigations).HasForeignKey(d => d.LetterFrom);

            entity.HasOne(d => d.PlantillaJobTitle).WithMany(p => p.Contracts).HasForeignKey(d => d.PlantillaJobTitleId);

            entity.HasOne(d => d.RecrtmntJobPostDetail).WithMany(p => p.Contracts).HasForeignKey(d => d.RecrtmntJobPostDetailId);
        });

        modelBuilder.Entity<Hr201allowance>(entity =>
        {
            entity.ToTable("HR201Allowance");

            entity.HasIndex(e => e.ContractId, "IX_HR201Allowance_ContractId");

            entity.HasIndex(e => e.Hr201allowanceTypeId, "IX_HR201Allowance_HR201AllowanceTypeId");

            entity.HasIndex(e => e.TenantId, "IX_HR201Allowance_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AllowanceVal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Hr201allowanceTypeId).HasColumnName("HR201AllowanceTypeId");

            entity.HasOne(d => d.Contract).WithMany(p => p.Hr201allowances).HasForeignKey(d => d.ContractId);

            entity.HasOne(d => d.Hr201allowanceType).WithMany(p => p.Hr201allowances).HasForeignKey(d => d.Hr201allowanceTypeId);
        });

        modelBuilder.Entity<Hr201allowanceType>(entity =>
        {
            entity.ToTable("HR201AllowanceTypes");

            entity.HasIndex(e => e.TenantId, "IX_HR201AllowanceTypes_TenantId");
        });

        modelBuilder.Entity<Hr201appointment>(entity =>
        {
            entity.ToTable("HR201Appointments");

            entity.HasIndex(e => e.Hr201appointmentTypeId, "IX_HR201Appointments_HR201AppointmentTypeId");

            entity.HasIndex(e => e.RecrtmntJobPostingDetailId, "IX_HR201Appointments_RecrtmntJobPostingDetailId");

            entity.HasIndex(e => e.TenantId, "IX_HR201Appointments_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Counter).HasColumnName("counter");
            entity.Property(e => e.Hr201appointmentTypeId).HasColumnName("HR201AppointmentTypeId");
            entity.Property(e => e.Message).HasMaxLength(2000);
            entity.Property(e => e.Name).HasMaxLength(1000);

            entity.HasOne(d => d.RecrtmntJobPostingDetail).WithMany(p => p.Hr201appointments).HasForeignKey(d => d.RecrtmntJobPostingDetailId);
        });

        modelBuilder.Entity<Hr201businessUnit>(entity =>
        {
            entity.ToTable("HR201BusinessUnits");

            entity.HasIndex(e => e.Hr201industryId, "IX_HR201BusinessUnits_HR201IndustryId");

            entity.HasIndex(e => e.OrganizationUnitId, "IX_HR201BusinessUnits_OrganizationUnitId");

            entity.HasIndex(e => e.TenantId, "IX_HR201BusinessUnits_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Hr201industryId).HasColumnName("HR201IndustryId");
            entity.Property(e => e.Name).HasMaxLength(500);
        });

        modelBuilder.Entity<Hr201civilStatus>(entity =>
        {
            entity.ToTable("HR201CivilStatuses");

            entity.HasIndex(e => e.TenantId, "IX_HR201CivilStatuses_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<Hr201employee>(entity =>
        {
            entity.ToTable("HR201Employees");

            entity.HasIndex(e => e.BinaryObjectId, "IX_HR201Employees_BinaryObjectId");

            entity.HasIndex(e => e.BirthPlaceAddressId, "IX_HR201Employees_BirthPlaceAddressId");

            entity.HasIndex(e => e.Hr201birStatId, "IX_HR201Employees_HR201BirStatId");

            entity.HasIndex(e => e.Hr201bloodTypeId, "IX_HR201Employees_HR201BloodTypeId");

            entity.HasIndex(e => e.Hr201businessUnitId, "IX_HR201Employees_HR201BusinessUnitId");

            entity.HasIndex(e => e.Hr201citizenshipId, "IX_HR201Employees_HR201CitizenshipId");

            entity.HasIndex(e => e.Hr201civilStatusId, "IX_HR201Employees_HR201CivilStatusId");

            entity.HasIndex(e => e.Hr201departmentId, "IX_HR201Employees_HR201DepartmentId");

            entity.HasIndex(e => e.Hr201designationId, "IX_HR201Employees_HR201DesignationId");

            entity.HasIndex(e => e.Hr201employmentStatId, "IX_HR201Employees_HR201EmploymentStatId");

            entity.HasIndex(e => e.Hr201genderId, "IX_HR201Employees_HR201GenderId");

            entity.HasIndex(e => e.Hr201locationId, "IX_HR201Employees_HR201LocationId");

            entity.HasIndex(e => e.Hr201religionId, "IX_HR201Employees_HR201ReligionId");

            entity.HasIndex(e => e.PresentAddressId, "IX_HR201Employees_PresentAddressId");

            entity.HasIndex(e => e.ProvincialAddressId, "IX_HR201Employees_ProvincialAddressId");

            entity.HasIndex(e => e.RecrtmntApplicantMasterlistId, "IX_HR201Employees_RecrtmntApplicantMasterlistId");

            entity.HasIndex(e => e.TenantId, "IX_HR201Employees_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.BirthPlaceAddressLegacyField)
                .HasMaxLength(1000)
                .HasColumnName("BirthPlaceAddress_LegacyField");
            entity.Property(e => e.CompanyEmailAddress).HasMaxLength(500);
            entity.Property(e => e.EmergencyContactName).HasMaxLength(500);
            entity.Property(e => e.EmergencyContactno).HasMaxLength(50);
            entity.Property(e => e.Firstname).HasMaxLength(500);
            entity.Property(e => e.Hdmfid)
                .HasMaxLength(50)
                .HasColumnName("HDMFId");
            entity.Property(e => e.Hr201birStatId).HasColumnName("HR201BirStatId");
            entity.Property(e => e.Hr201bloodTypeId).HasColumnName("HR201BloodTypeId");
            entity.Property(e => e.Hr201businessUnitId).HasColumnName("HR201BusinessUnitId");
            entity.Property(e => e.Hr201citizenshipId).HasColumnName("HR201CitizenshipId");
            entity.Property(e => e.Hr201civilStatusId).HasColumnName("HR201CivilStatusId");
            entity.Property(e => e.Hr201departmentId).HasColumnName("HR201DepartmentId");
            entity.Property(e => e.Hr201designationId).HasColumnName("HR201DesignationId");
            entity.Property(e => e.Hr201employmentStatId).HasColumnName("HR201EmploymentStatId");
            entity.Property(e => e.Hr201genderId).HasColumnName("HR201GenderId");
            entity.Property(e => e.Hr201locationId).HasColumnName("HR201LocationId");
            entity.Property(e => e.Hr201religionId).HasColumnName("HR201ReligionId");
            entity.Property(e => e.Lastname).HasMaxLength(500);
            entity.Property(e => e.Middlename).HasMaxLength(500);
            entity.Property(e => e.MobileNo).HasMaxLength(13);
            entity.Property(e => e.Nickname).HasMaxLength(200);
            entity.Property(e => e.PersonalEmailAddress).HasMaxLength(500);
            entity.Property(e => e.PhicId).HasMaxLength(50);
            entity.Property(e => e.PresentAddressLegacyField)
                .HasMaxLength(1000)
                .HasColumnName("PresentAddress_LegacyField");
            entity.Property(e => e.ProvincialAddressLegacyField)
                .HasMaxLength(1000)
                .HasColumnName("ProvincialAddress_LegacyField");
            entity.Property(e => e.Sssid)
                .HasMaxLength(50)
                .HasColumnName("SSSId");
            entity.Property(e => e.SuffixName).HasMaxLength(200);
            entity.Property(e => e.Tin).HasMaxLength(50);

            entity.HasOne(d => d.BinaryObject).WithMany(p => p.Hr201employees).HasForeignKey(d => d.BinaryObjectId);

            entity.HasOne(d => d.Hr201businessUnit).WithMany(p => p.Hr201employees).HasForeignKey(d => d.Hr201businessUnitId);

            entity.HasOne(d => d.Hr201civilStatus).WithMany(p => p.Hr201employees).HasForeignKey(d => d.Hr201civilStatusId);

            entity.HasOne(d => d.Hr201gender).WithMany(p => p.Hr201employees).HasForeignKey(d => d.Hr201genderId);

            entity.HasOne(d => d.Hr201location).WithMany(p => p.Hr201employees).HasForeignKey(d => d.Hr201locationId);

            entity.HasOne(d => d.RecrtmntApplicantMasterlist).WithMany(p => p.Hr201employees).HasForeignKey(d => d.RecrtmntApplicantMasterlistId);
        });

        modelBuilder.Entity<Hr201gender>(entity =>
        {
            entity.ToTable("HR201Genders");

            entity.HasIndex(e => e.TenantId, "IX_HR201Genders_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Hr201location>(entity =>
        {
            entity.ToTable("HR201Locations");

            entity.HasIndex(e => e.Hr201barangayId, "IX_HR201Locations_HR201BarangayId");

            entity.HasIndex(e => e.TenantId, "IX_HR201Locations_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CompleteAddress).HasMaxLength(2000);
            entity.Property(e => e.DailyRatePerArea).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Hr201barangayId).HasColumnName("HR201BarangayId");
            entity.Property(e => e.Name).HasMaxLength(500);
        });

        modelBuilder.Entity<Mrdetail>(entity =>
        {
            entity.ToTable("MRDetails");

            entity.HasIndex(e => e.Hr201allowanceTypeId, "IX_MRDetails_HR201AllowanceTypeId");

            entity.HasIndex(e => e.MrheaderId, "IX_MRDetails_MRHeaderId");

            entity.HasIndex(e => e.PlantillaApprovedMasterlistId, "IX_MRDetails_PlantillaApprovedMasterlistId");

            entity.HasIndex(e => e.PlantillaJobMngmntId, "IX_MRDetails_PlantillaJobMngmntId");

            entity.HasIndex(e => e.PrevEmployeeId, "IX_MRDetails_PrevEmployeeId");

            entity.HasIndex(e => e.TenantId, "IX_MRDetails_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Allowance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ExistingMrfsheet).HasColumnName("existingMRFSheet");
            entity.Property(e => e.Hr201allowanceTypeId).HasColumnName("HR201AllowanceTypeId");
            entity.Property(e => e.MrheaderId).HasColumnName("MRHeaderId");

            entity.HasOne(d => d.Hr201allowanceType).WithMany(p => p.Mrdetails).HasForeignKey(d => d.Hr201allowanceTypeId);

            entity.HasOne(d => d.PlantillaJobMngmnt).WithMany(p => p.Mrdetails).HasForeignKey(d => d.PlantillaJobMngmntId);

            entity.HasOne(d => d.PrevEmployee).WithMany(p => p.Mrdetails).HasForeignKey(d => d.PrevEmployeeId);
        });

        modelBuilder.Entity<PlantillaJobMngmnt>(entity =>
        {
            entity.HasIndex(e => e.Hr201businessUnitId, "IX_PlantillaJobMngmnts_HR201BusinessUnitId");

            entity.HasIndex(e => e.Hr201departmentId, "IX_PlantillaJobMngmnts_HR201DepartmentId");

            entity.HasIndex(e => e.Hr201locationId, "IX_PlantillaJobMngmnts_HR201LocationId");

            entity.HasIndex(e => e.Hr201unitId, "IX_PlantillaJobMngmnts_HR201UnitId");

            entity.HasIndex(e => e.JobTitleReportToId, "IX_PlantillaJobMngmnts_JobTitleReportToId");

            entity.HasIndex(e => e.MrjobSourceId, "IX_PlantillaJobMngmnts_MRJobSourceId");

            entity.HasIndex(e => e.MrjobTypeId, "IX_PlantillaJobMngmnts_MRJobTypeId");

            entity.HasIndex(e => e.PlantillaCategoryId, "IX_PlantillaJobMngmnts_PlantillaCategoryId");

            entity.HasIndex(e => e.PlantillaJobCategoryId, "IX_PlantillaJobMngmnts_PlantillaJobCategoryId");

            entity.HasIndex(e => e.PlantillaJobTitleId, "IX_PlantillaJobMngmnts_PlantillaJobTitleId");

            entity.HasIndex(e => e.PlantillaRankId, "IX_PlantillaJobMngmnts_PlantillaRankId");

            entity.HasIndex(e => e.TenantId, "IX_PlantillaJobMngmnts_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Hr201businessUnitId).HasColumnName("HR201BusinessUnitId");
            entity.Property(e => e.Hr201departmentId).HasColumnName("HR201DepartmentId");
            entity.Property(e => e.Hr201locationId).HasColumnName("HR201LocationId");
            entity.Property(e => e.Hr201unitId).HasColumnName("HR201UnitId");
            entity.Property(e => e.IsDepartmentHead).HasColumnName("isDepartmentHead");
            entity.Property(e => e.MrjobSourceId).HasColumnName("MRJobSourceId");
            entity.Property(e => e.MrjobTypeId).HasColumnName("MRJobTypeId");

            entity.HasOne(d => d.Hr201businessUnit).WithMany(p => p.PlantillaJobMngmnts).HasForeignKey(d => d.Hr201businessUnitId);

            entity.HasOne(d => d.Hr201location).WithMany(p => p.PlantillaJobMngmnts).HasForeignKey(d => d.Hr201locationId);

            entity.HasOne(d => d.JobTitleReportTo).WithMany(p => p.InverseJobTitleReportTo).HasForeignKey(d => d.JobTitleReportToId);

            entity.HasOne(d => d.PlantillaJobTitle).WithMany(p => p.PlantillaJobMngmnts).HasForeignKey(d => d.PlantillaJobTitleId);

            entity.HasOne(d => d.PlantillaRank).WithMany(p => p.PlantillaJobMngmnts).HasForeignKey(d => d.PlantillaRankId);
        });

        modelBuilder.Entity<PlantillaJobTitle>(entity =>
        {
            entity.HasIndex(e => e.TenantId, "IX_PlantillaJobTitles_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<PlantillaRank>(entity =>
        {
            entity.HasIndex(e => e.Hr201businessUnitId, "IX_PlantillaRanks_HR201BusinessUnitId");

            entity.HasIndex(e => e.Hr201locationId, "IX_PlantillaRanks_HR201LocationId");

            entity.HasIndex(e => e.PlantillaBandId, "IX_PlantillaRanks_PlantillaBandId");

            entity.HasIndex(e => e.PlantillaRankLvlId, "IX_PlantillaRanks_PlantillaRankLvlId");

            entity.HasIndex(e => e.TenantId, "IX_PlantillaRanks_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Hr201businessUnitId).HasColumnName("HR201BusinessUnitId");
            entity.Property(e => e.Hr201locationId).HasColumnName("HR201LocationId");
            entity.Property(e => e.MaxRate).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MidRate).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MinRate).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Hr201businessUnit).WithMany(p => p.PlantillaRanks).HasForeignKey(d => d.Hr201businessUnitId);

            entity.HasOne(d => d.Hr201location).WithMany(p => p.PlantillaRanks).HasForeignKey(d => d.Hr201locationId);
        });

        modelBuilder.Entity<RecrtmntApplcntEducDetail>(entity =>
        {
            entity.HasIndex(e => e.RecrtmntApplcntEducHeaderId, "IX_RecrtmntApplcntEducDetails_RecrtmntApplcntEducHeaderId");

            entity.HasIndex(e => e.TenantId, "IX_RecrtmntApplcntEducDetails_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.SchoolName).HasMaxLength(500);

            entity.HasOne(d => d.RecrtmntApplcntEducHeader).WithMany(p => p.RecrtmntApplcntEducDetails).HasForeignKey(d => d.RecrtmntApplcntEducHeaderId);
        });

        modelBuilder.Entity<RecrtmntApplcntEducHeader>(entity =>
        {
            entity.HasIndex(e => e.RecrtmntApplicantMasterlistId, "IX_RecrtmntApplcntEducHeaders_RecrtmntApplicantMasterlistId");

            entity.HasIndex(e => e.TenantId, "IX_RecrtmntApplcntEducHeaders_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.RecrtmntApplicantMasterlist).WithMany(p => p.RecrtmntApplcntEducHeaders).HasForeignKey(d => d.RecrtmntApplicantMasterlistId);
        });

        modelBuilder.Entity<RecrtmntApplicantMasterlist>(entity =>
        {
            entity.HasIndex(e => e.BinaryObjectId, "IX_RecrtmntApplicantMasterlists_BinaryObjectId");

            entity.HasIndex(e => e.Hr201civilStatusId, "IX_RecrtmntApplicantMasterlists_HR201CivilStatusId");

            entity.HasIndex(e => e.Hr201genderId, "IX_RecrtmntApplicantMasterlists_HR201GenderId");

            entity.HasIndex(e => e.Hr201religionId, "IX_RecrtmntApplicantMasterlists_HR201ReligionId");

            entity.HasIndex(e => e.TenantId, "IX_RecrtmntApplicantMasterlists_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.BgcheckStatus).HasColumnName("BGCheckStatus");
            entity.Property(e => e.Birnumber)
                .HasMaxLength(12)
                .HasColumnName("BIRNumber");
            entity.Property(e => e.EmailAddress).HasMaxLength(1000);
            entity.Property(e => e.FirstName).HasMaxLength(1000);
            entity.Property(e => e.Hdmfnumber)
                .HasMaxLength(12)
                .HasColumnName("HDMFNumber");
            entity.Property(e => e.Hr201civilStatusId).HasColumnName("HR201CivilStatusId");
            entity.Property(e => e.Hr201genderId).HasColumnName("HR201GenderId");
            entity.Property(e => e.Hr201religionId).HasColumnName("HR201ReligionId");
            entity.Property(e => e.IsBuInitiated).HasColumnName("isBuInitiated");
            entity.Property(e => e.IsInternal).HasColumnName("isInternal");
            entity.Property(e => e.LastName).HasMaxLength(1000);
            entity.Property(e => e.MiddleName).HasMaxLength(1000);
            entity.Property(e => e.PhilHealthNum).HasMaxLength(11);
            entity.Property(e => e.Sssnumber)
                .HasMaxLength(10)
                .HasColumnName("SSSNumber");
            entity.Property(e => e.TinNumber).HasMaxLength(12);

            entity.HasOne(d => d.BinaryObject).WithMany(p => p.RecrtmntApplicantMasterlists).HasForeignKey(d => d.BinaryObjectId);

            entity.HasOne(d => d.Hr201civilStatus).WithMany(p => p.RecrtmntApplicantMasterlists).HasForeignKey(d => d.Hr201civilStatusId);

            entity.HasOne(d => d.Hr201gender).WithMany(p => p.RecrtmntApplicantMasterlists).HasForeignKey(d => d.Hr201genderId);
        });

        modelBuilder.Entity<RecrtmntExperienceDetail>(entity =>
        {
            entity.HasIndex(e => e.RecrtmntExperienceHeaderId, "IX_RecrtmntExperienceDetails_RecrtmntExperienceHeaderId");

            entity.HasIndex(e => e.TenantId, "IX_RecrtmntExperienceDetails_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CompanyAddress).HasMaxLength(500);
            entity.Property(e => e.CompanyName).HasMaxLength(500);
            entity.Property(e => e.IsPresent).HasColumnName("isPresent");
            entity.Property(e => e.JobDescription).HasMaxLength(500);
            entity.Property(e => e.JobTitle).HasMaxLength(500);

            entity.HasOne(d => d.RecrtmntExperienceHeader).WithMany(p => p.RecrtmntExperienceDetails).HasForeignKey(d => d.RecrtmntExperienceHeaderId);
        });

        modelBuilder.Entity<RecrtmntExperienceHeader>(entity =>
        {
            entity.HasIndex(e => e.RecrtmntApplicantMasterlistId, "IX_RecrtmntExperienceHeaders_RecrtmntApplicantMasterlistId");

            entity.HasIndex(e => e.TenantId, "IX_RecrtmntExperienceHeaders_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(500);

            entity.HasOne(d => d.RecrtmntApplicantMasterlist).WithMany(p => p.RecrtmntExperienceHeaders).HasForeignKey(d => d.RecrtmntApplicantMasterlistId);
        });

        modelBuilder.Entity<RecrtmntJobAdvertisement>(entity =>
        {
            entity.HasIndex(e => e.BinaryObjectId, "IX_RecrtmntJobAdvertisements_BinaryObjectId");

            entity.HasIndex(e => e.MrdetailId, "IX_RecrtmntJobAdvertisements_MRDetailId");

            entity.HasIndex(e => e.TenantId, "IX_RecrtmntJobAdvertisements_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.MrdetailId).HasColumnName("MRDetailId");

            entity.HasOne(d => d.BinaryObject).WithMany(p => p.RecrtmntJobAdvertisements).HasForeignKey(d => d.BinaryObjectId);

            entity.HasOne(d => d.Mrdetail).WithMany(p => p.RecrtmntJobAdvertisements).HasForeignKey(d => d.MrdetailId);
        });

        modelBuilder.Entity<RecrtmntJobPostingDetail>(entity =>
        {
            entity.HasIndex(e => e.Hr201employeeId, "IX_RecrtmntJobPostingDetails_HR201EmployeeId");

            entity.HasIndex(e => e.PlantillaJobTitleId, "IX_RecrtmntJobPostingDetails_PlantillaJobTitleId");

            entity.HasIndex(e => e.RecrtmntApplicantMasterlistId, "IX_RecrtmntJobPostingDetails_RecrtmntApplicantMasterlistId");

            entity.HasIndex(e => e.RecrtmntJobPostingHeaderId, "IX_RecrtmntJobPostingDetails_RecrtmntJobPostingHeaderId");

            entity.HasIndex(e => e.TenantId, "IX_RecrtmntJobPostingDetails_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ApplicantNo).HasMaxLength(500);
            entity.Property(e => e.Hr201employeeId).HasColumnName("HR201EmployeeId");

            entity.HasOne(d => d.Hr201employee).WithMany(p => p.RecrtmntJobPostingDetails).HasForeignKey(d => d.Hr201employeeId);

            entity.HasOne(d => d.PlantillaJobTitle).WithMany(p => p.RecrtmntJobPostingDetails).HasForeignKey(d => d.PlantillaJobTitleId);

            entity.HasOne(d => d.RecrtmntApplicantMasterlist).WithMany(p => p.RecrtmntJobPostingDetails).HasForeignKey(d => d.RecrtmntApplicantMasterlistId);

            entity.HasOne(d => d.RecrtmntJobPostingHeader).WithMany(p => p.RecrtmntJobPostingDetails).HasForeignKey(d => d.RecrtmntJobPostingHeaderId);
        });

        modelBuilder.Entity<RecrtmntJobPostingDetailAuditLog>(entity =>
        {
            entity.HasIndex(e => e.RecrtmntJobPostingDetailId, "IX_RecrtmntJobPostingDetailAuditLogs_RecrtmntJobPostingDetailId");

            entity.HasIndex(e => e.TenantId, "IX_RecrtmntJobPostingDetailAuditLogs_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.RecrtmntJobPostingDetail).WithMany(p => p.RecrtmntJobPostingDetailAuditLogs).HasForeignKey(d => d.RecrtmntJobPostingDetailId);
        });

        modelBuilder.Entity<RecrtmntJobPostingHeader>(entity =>
        {
            entity.HasIndex(e => e.MrdetailId, "IX_RecrtmntJobPostingHeaders_MRDetailId");

            entity.HasIndex(e => e.TenantId, "IX_RecrtmntJobPostingHeaders_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.MrdetailId).HasColumnName("MRDetailId");

            entity.HasOne(d => d.Mrdetail).WithMany(p => p.RecrtmntJobPostingHeaders).HasForeignKey(d => d.MrdetailId);
        });

        modelBuilder.Entity<RecrtmntJobPostingReqChecklist>(entity =>
        {
            entity.HasIndex(e => e.RecrtmntJobPostingDetailId, "IX_RecrtmntJobPostingReqChecklists_RecrtmntJobPostingDetailId");

            entity.HasIndex(e => e.RecrtmntRequirementChecklistId, "IX_RecrtmntJobPostingReqChecklists_RecrtmntRequirementChecklistId");

            entity.HasIndex(e => e.TenantId, "IX_RecrtmntJobPostingReqChecklists_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.RecrtmntJobPostingDetail).WithMany(p => p.RecrtmntJobPostingReqChecklists).HasForeignKey(d => d.RecrtmntJobPostingDetailId);

            entity.HasOne(d => d.RecrtmntRequirementChecklist).WithMany(p => p.RecrtmntJobPostingReqChecklists).HasForeignKey(d => d.RecrtmntRequirementChecklistId);
        });

        modelBuilder.Entity<RecrtmntJobPostingheaderAuditLog>(entity =>
        {
            entity.HasIndex(e => e.RecrtmntJobPostingHeaderId, "IX_RecrtmntJobPostingheaderAuditLogs_RecrtmntJobPostingHeaderId");

            entity.HasIndex(e => e.TenantId, "IX_RecrtmntJobPostingheaderAuditLogs_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.RecrtmntJobPostingHeader).WithMany(p => p.RecrtmntJobPostingheaderAuditLogs).HasForeignKey(d => d.RecrtmntJobPostingHeaderId);
        });

        modelBuilder.Entity<RecrtmntRequirementChecklist>(entity =>
        {
            entity.HasIndex(e => e.Hr201employmentStatId, "IX_RecrtmntRequirementChecklists_HR201EmploymentStatId");

            entity.HasIndex(e => e.TenantId, "IX_RecrtmntRequirementChecklists_TenantId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Hr201employmentStatId).HasColumnName("HR201EmploymentStatId");
            entity.Property(e => e.Name).HasMaxLength(1000);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
