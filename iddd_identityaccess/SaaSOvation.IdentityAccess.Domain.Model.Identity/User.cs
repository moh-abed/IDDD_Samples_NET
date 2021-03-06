﻿namespace SaaSOvation.IdentityAccess.Domain.Model.Identity
{
    using System;
    using SaaSOvation.Common.Domain.Model;

    public class User : AssertionConcern
    {
        internal User(
                Identity<Tenant> tenantId,
                string username,
                string password,
                Enablement enablement,
                Person person)

            : this()
        {

            this.Enablement = enablement;
            this.Person = person;
            this.TenantId = tenantId;
            this.Username = username;

            this.ProtectPassword("", password);

            person.User = this;

            DomainEventPublisher
                .Instance
                .Publish(new UserRegistered(
                        tenantId,
                        username,
                        person.Name,
                        person.ContactInformation.EmailAddress));
        }

        internal User()
        {
        }

        public bool Enabled
        {
            get
            {
                return this.Enablement.IsEnablementEnabled();
            }
        }

        public Enablement Enablement { get; private set; }

        public string Password { get; internal set; }

        public Person Person { get; private set; }

        public Identity<Tenant> TenantId { get; private set; }

        public UserDescriptor UserDescriptor
        {
            get
            {
                return new UserDescriptor(
                        this.TenantId,
                        this.Username,
                        this.Person.EmailAddress.Address);
            }
        }

        public string Username { get; private set; }

        public void ChangePassword(string currentPassword, string changedPassword)
        {
            this.AssertArgumentNotEmpty(
                    currentPassword,
                    "Current and new password must be provided.");

            this.AssertArgumentEquals(
                    this.Password,
                    this.AsEncryptedValue(currentPassword),
                    "Current password not confirmed.");

            this.ProtectPassword(currentPassword, changedPassword);

            DomainEventPublisher.Instance.Publish(new UserPasswordChanged(this.TenantId, this.Username));
        }

        public void ChangePersonalContactInformation(ContactInformation contactInformation)
        {
            this.Person.ChangeContactInformation(contactInformation);
        }

        public void ChangePersonalName(FullName personalName)
        {
            this.Person.ChangeName(personalName);
        }

        public void DefineEnablement(Enablement enablement)
        {
            this.Enablement = enablement;

            DomainEventPublisher
                .Instance
                .Publish(new UserEnablementChanged(
                        this.TenantId,
                        this.Username,
                        this.Enablement));
        }

        public bool IsEnabled()
        {
            return this.Enablement.IsEnablementEnabled();
        }

        public override bool Equals(object anotherObject)
        {
            bool equalobjects = false;

            if (anotherObject != null && this.GetType() == anotherObject.GetType())
            {
                User typedobject = (User) anotherObject;
                equalobjects =
                    this.TenantId.Equals(typedobject.TenantId) &&
                    this.Username.Equals(typedobject.Username);
            }

            return equalobjects;
        }

        public override int GetHashCode()
        {
            int hashCodeValue =
                + (45217 * 269)
                + this.TenantId.GetHashCode()
                + this.Username.GetHashCode();

            return hashCodeValue;
        }

        public override string ToString()
        {
            return "User [tenantId=" + TenantId + ", username=" + Username
                    + ", person=" + Person + ", enablement=" + Enablement + "]";
        }

        internal GroupMember ToGroupMember()
        {
            GroupMember groupMember =
                new GroupMember(
                        this.TenantId,
                        this.Username,
                        GroupMemberType.User);

            return groupMember;
        }

        private string AsEncryptedValue(string plainTextPassword)
        {
            string encryptedValue =
                DomainRegistry
                    .EncryptionService
                    .EncryptedValue(plainTextPassword);

            return encryptedValue;
        }

        private void AssertPasswordsNotSame(string currentPassword, string changedPassword)
        {
            this.AssertArgumentNotEquals(currentPassword, changedPassword, "The password is unchanged.");
        }

        private void AssertPasswordNotWeak(string plainTextPassword)
        {
            this.AssertArgumentFalse(
                    DomainRegistry.PasswordService.IsWeak(plainTextPassword),
                    "The password must be stronger.");
        }

        private void AssertUsernamePasswordNotSame(string plainTextPassword)
        {
            this.AssertArgumentNotEquals(
                    this.Username,
                    plainTextPassword,
                    "The username and password must not be the same.");
        }

        private void ProtectPassword(string currentPassword, string changedPassword)
        {
            this.AssertPasswordsNotSame(currentPassword, changedPassword);

            this.AssertPasswordNotWeak(changedPassword);

            this.AssertUsernamePasswordNotSame(changedPassword);

            this.Password = this.AsEncryptedValue(changedPassword);
        }
    }
}
