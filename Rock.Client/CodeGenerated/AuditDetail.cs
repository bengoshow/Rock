//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the Rock.CodeGeneration project
//     Changes to this file will be lost when the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;


namespace Rock.Client
{
    /// <summary>
    /// Base client model for AuditDetail that only includes the non-virtual fields. Use this for PUT/POSTs
    /// </summary>
    public partial class AuditDetailEntity
    {
        /// <summary />
        public int Id { get; set; }

        /// <summary />
        public int AuditId { get; set; }

        /// <summary />
        public string CurrentValue { get; set; }

        /// <summary />
        public Guid? ForeignGuid { get; set; }

        /// <summary />
        public string ForeignKey { get; set; }

        /// <summary />
        public string OriginalValue { get; set; }

        /// <summary />
        public string Property { get; set; }

        /// <summary />
        public Guid Guid { get; set; }

        /// <summary />
        public int? ForeignId { get; set; }

        /// <summary>
        /// Copies the base properties from a source AuditDetail object
        /// </summary>
        /// <param name="source">The source.</param>
        public void CopyPropertiesFrom( AuditDetail source )
        {
            this.Id = source.Id;
            this.AuditId = source.AuditId;
            this.CurrentValue = source.CurrentValue;
            this.ForeignGuid = source.ForeignGuid;
            this.ForeignKey = source.ForeignKey;
            this.OriginalValue = source.OriginalValue;
            this.Property = source.Property;
            this.Guid = source.Guid;
            this.ForeignId = source.ForeignId;

        }
    }

    /// <summary>
    /// Client model for AuditDetail that includes all the fields that are available for GETs. Use this for GETs (use AuditDetailEntity for POST/PUTs)
    /// </summary>
    public partial class AuditDetail : AuditDetailEntity
    {
    }
}
