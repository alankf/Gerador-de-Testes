CREATE TABLE [dbo].[TBTests] (
    [Id]         INT           IDENTITY (1, 1) NOT NULL,
    [Title]      VARCHAR (200) NOT NULL,
    [Grade]      VARCHAR (200) NOT NULL,
    [Bimester]   INT           NOT NULL,
    [Materia_Id] INT           NOT NULL,
    CONSTRAINT [PK_TBTests] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_TBTests_TBMateria] FOREIGN KEY ([Materia_Id]) REFERENCES [dbo].[TBMaterias] ([Id])
);

