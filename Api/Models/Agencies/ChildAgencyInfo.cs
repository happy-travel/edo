using System;
using System.Collections.Generic;

namespace HappyTravel.Edo.Api.Models.Agencies
{
    public readonly struct ChildAgencyInfo
    {
        /// <summary>
        /// Agency id
        /// </summary>
        public int Id { get; init; }
        
        /// <summary>
        /// Agency name
        /// </summary>
        public string Name { get; init;}
        
        /// <summary>
        /// Activity state
        /// </summary>
        public bool IsActive { get; init;}
        
        /// <summary>
        /// Created date
        /// </summary>
        public DateTime Created { get; init;}
        
        /// <summary>
        /// Virtual accounts
        /// </summary>
        public List<AgencyAccountInfo> Accounts { get; init; }
        
        
        public bool Equals(ChildAgencyInfo other) 
            => Id == other.Id;


        public override bool Equals(object obj) 
            => obj is ChildAgencyInfo other && Equals(other);


        public override int GetHashCode() 
            => Id;
    }
}