using SnpDepotManagement.Domain.Common;
using SnpDepotManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SnpDepotManagement.Domain.Entities.Containers;
public sealed class Container : AuditableEntity
{
    public string ContainerNumber { get; private set; } = string.Empty;
    public string IsoContainer { get; private set; } = string.Empty;
    public ContainerSize Size { get; private set; }
    public ContainerType Type { get; private set; }
    public decimal MaxWeight { get; private set; }
    public decimal TareWeight { get; private set; }
    public DateTime? DateOfManuFacture { get; private set; }
    public string OwnerCode { get; private set; } = string.Empty;

    private Container() { } // EF Core requires a parameterless constructor for entity materialization

    public static Container Create(
        string containerNumber,
            string isoCode,
            ContainerSize size,
            ContainerType type,
            decimal maxWeight,
            decimal tareWeight,
            DateTime? dateOfManufacture,
            string ownerCode)
    {
        return new Container
        {
            ContainerNumber = containerNumber,
            IsoContainer = isoCode,
            Size = size,
            Type = type,
            MaxWeight = maxWeight,
            TareWeight = tareWeight,
            DateOfManuFacture = dateOfManufacture,
            OwnerCode = ownerCode
        };
    }
}