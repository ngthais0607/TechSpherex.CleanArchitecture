namespace SnpDepotManagement.Domain.Enums;

public enum ContainerSize
{
    Ft20 = 20,
    Ft40 = 40
}

public enum ContainerType
{
    Dry = 'U',
    Reefer = 'R',
    OpenTop = 'S',
    FlatRack = 'F',
    Bunker = 'B',
    Ventilated = 'V',
    Specialized = 'Z'
}

public enum Classification
{
    A = 1,
    B = 2,
    C = 3
}

public enum ContainerClassification
{
    A = 1,
    B = 2,
    C = 3,
    GradeA = 1,
    GradeB = 2,
    GradeC = 3
}

public enum ContainerCondition
{
    Normal,
    Damaged,
    Dented,
    Bent
}