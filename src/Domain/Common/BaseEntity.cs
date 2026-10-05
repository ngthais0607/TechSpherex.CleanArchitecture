using System;
using System.Collections.Generic;
using System.Text;

namespace SnpDepotManagement.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();     
}

