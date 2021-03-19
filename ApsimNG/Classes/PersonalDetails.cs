using System;

namespace UserInterface
{
    /// <summary>
    /// Details required when registering/upgrading
    /// to a new version of apsim.
    /// </summary>
    public class PersonalDetails
    {
        /// <summary>
        /// The person's first name.
        /// </summary>
        public string FirstName { get; private set; }

        /// <summary>
        /// The person's last name.
        /// </summary>
        public string LastName { get; private set; }

        /// <summary>
        /// The person's email address.
        /// </summary>
        public string EmailAddress { get; set; }

        /// <summary>
        /// The person's organisation/company (optional).
        /// </summary>
        public string Organisation { get; set; }

        /// <summary>
        /// The person's contry of residence.
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="first">First name.</param>
        /// <param name="last">Last name.</param>
        /// <param name="email">Email address.</param>
        /// <param name="country">Country of residence.</param>
        /// <param name="organisation">Organisation/company (optional).</param>
        public PersonalDetails(string first, string last, string email, string country, string organisation = null)
        {
            FirstName = first;
            LastName = last;
            EmailAddress = email;
            Organisation = organisation;
            Country = country;
        }

        /// <summary>
        /// Validate the details. Will throw iff invalid.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrEmpty(FirstName))
                throw new ArgumentNullException("First Name");
            if (string.IsNullOrEmpty(LastName))
                throw new ArgumentNullException("Last Name");
            if (string.IsNullOrEmpty(EmailAddress))
                throw new ArgumentNullException("Email Address");
            if (string.IsNullOrEmpty(Organisation))
                throw new ArgumentNullException("Country");
        }
    }
}