IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Users
    WHERE NormalizedLogin = 'STUDENT'
)
BEGIN
    INSERT INTO dbo.Users (Id, Login, NormalizedLogin, PasswordHash)
    VALUES
    (
        '8D11C893-63C2-4C72-93C8-E9329D9A8EE8',
        'student',
        'STUDENT',
        '600000:c3R1ZGVudC1zZWVkLXYxIQ==:iC42+OEa6bTa4+8NTTMy0qVY668LScq7d8lHVy63YoA='
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Subjects WHERE Id = '4DDC929A-5B93-4DB0-BEE0-274F5302EF75')
BEGIN
    INSERT INTO dbo.Subjects (Id, Name, Description, ParentSubjectId)
    VALUES
    (
        '4DDC929A-5B93-4DB0-BEE0-274F5302EF75',
        'Computer Science',
        'Core concepts for software design and problem solving.',
        NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Subjects WHERE Id = 'D9674C7B-D6AC-44A2-9531-E22972518F10')
BEGIN
    INSERT INTO dbo.Subjects (Id, Name, Description, ParentSubjectId)
    VALUES
    (
        'D9674C7B-D6AC-44A2-9531-E22972518F10',
        'C# Fundamentals',
        'Language features, types, and object-oriented programming.',
        '4DDC929A-5B93-4DB0-BEE0-274F5302EF75'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Subjects WHERE Id = '7C9C7D9A-A772-4A96-B374-A3B4750FA0F2')
BEGIN
    INSERT INTO dbo.Subjects (Id, Name, Description, ParentSubjectId)
    VALUES
    (
        '7C9C7D9A-A772-4A96-B374-A3B4750FA0F2',
        'Databases',
        'Relational modeling and practical SQL queries.',
        '4DDC929A-5B93-4DB0-BEE0-274F5302EF75'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Subjects WHERE Id = '67010711-B6F1-452B-9D6B-21F7FCE8C00D')
BEGIN
    INSERT INTO dbo.Subjects (Id, Name, Description, ParentSubjectId)
    VALUES
    (
        '67010711-B6F1-452B-9D6B-21F7FCE8C00D',
        'Learning Systems',
        'Practice methods for retaining and applying new knowledge.',
        NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.StudyNotes WHERE Id = 'B3B8371A-5F7D-4D0C-BA4E-BC8FC5D2D406')
BEGIN
    INSERT INTO dbo.StudyNotes (Id, SubjectId, Title, Content, StudyDurationTicks, StudyStartedAtUtc)
    VALUES
    (
        'B3B8371A-5F7D-4D0C-BA4E-BC8FC5D2D406',
        'D9674C7B-D6AC-44A2-9531-E22972518F10',
        'Value types and reference types',
        'Value types store their data directly, while reference types store a reference to an object. Copying each kind has different effects.',
        27000000000,
        '2026-08-04T18:00:00+00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.StudyNotes WHERE Id = '9C59F504-BFE7-4BA8-A961-312090294BFD')
BEGIN
    INSERT INTO dbo.StudyNotes (Id, SubjectId, Title, Content, StudyDurationTicks, StudyStartedAtUtc)
    VALUES
    (
        '9C59F504-BFE7-4BA8-A961-312090294BFD',
        'D9674C7B-D6AC-44A2-9531-E22972518F10',
        'Composition over inheritance',
        'Use composition when a type needs capabilities that can vary independently. Inheritance is best reserved for a stable is-a relationship.',
        45000000000,
        '2026-08-06T18:30:00+00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.StudyNotes WHERE Id = 'D1935CF8-F5AD-4A26-AD67-61FAD368BB12')
BEGIN
    INSERT INTO dbo.StudyNotes (Id, SubjectId, Title, Content, StudyDurationTicks, StudyStartedAtUtc)
    VALUES
    (
        'D1935CF8-F5AD-4A26-AD67-61FAD368BB12',
        '7C9C7D9A-A772-4A96-B374-A3B4750FA0F2',
        'Primary keys and foreign keys',
        'A primary key identifies each row. A foreign key preserves a relationship by referencing a valid row in another table.',
        21000000000,
        '2026-08-07T17:15:00+00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.StudyNotes WHERE Id = '4839FE22-01C6-48A9-941D-AE21BE89C71D')
BEGIN
    INSERT INTO dbo.StudyNotes (Id, SubjectId, Title, Content, StudyDurationTicks, StudyStartedAtUtc)
    VALUES
    (
        '4839FE22-01C6-48A9-941D-AE21BE89C71D',
        '67010711-B6F1-452B-9D6B-21F7FCE8C00D',
        'Active recall session',
        'Close the notes and explain the idea from memory before checking what was missed. The missed pieces become the next review prompt.',
        33000000000,
        '2026-08-09T14:00:00+00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.SubjectConnections WHERE Id = 'F7DBD938-11D5-47F1-BC22-3DA692E16BE2')
BEGIN
    INSERT INTO dbo.SubjectConnections (Id, SubjectId, ConnectedSubjectId)
    VALUES
    (
        'F7DBD938-11D5-47F1-BC22-3DA692E16BE2',
        'D9674C7B-D6AC-44A2-9531-E22972518F10',
        '7C9C7D9A-A772-4A96-B374-A3B4750FA0F2'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.SubjectConnections WHERE Id = '75DFF247-855C-42AB-A72F-2EBEFAF62C89')
BEGIN
    INSERT INTO dbo.SubjectConnections (Id, SubjectId, ConnectedSubjectId)
    VALUES
    (
        '75DFF247-855C-42AB-A72F-2EBEFAF62C89',
        '67010711-B6F1-452B-9D6B-21F7FCE8C00D',
        'D9674C7B-D6AC-44A2-9531-E22972518F10'
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.StudyNoteMetrics
    WHERE StudyNoteId = 'B3B8371A-5F7D-4D0C-BA4E-BC8FC5D2D406'
      AND MetricDefinitionId = 'B2B182D0-8709-4328-BDA1-0A73B51D0E82'
)
BEGIN
    INSERT INTO dbo.StudyNoteMetrics (StudyNoteId, MetricDefinitionId, MetricValue)
    VALUES ('B3B8371A-5F7D-4D0C-BA4E-BC8FC5D2D406', 'B2B182D0-8709-4328-BDA1-0A73B51D0E82', 18);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.StudyNoteMetrics
    WHERE StudyNoteId = '9C59F504-BFE7-4BA8-A961-312090294BFD'
      AND MetricDefinitionId = '6D584D3A-6D8E-4B7A-A9AF-2C52C90DAA5E'
)
BEGIN
    INSERT INTO dbo.StudyNoteMetrics (StudyNoteId, MetricDefinitionId, MetricValue)
    VALUES ('9C59F504-BFE7-4BA8-A961-312090294BFD', '6D584D3A-6D8E-4B7A-A9AF-2C52C90DAA5E', 12);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.StudyNoteMetrics
    WHERE StudyNoteId = 'D1935CF8-F5AD-4A26-AD67-61FAD368BB12'
      AND MetricDefinitionId = 'B2B182D0-8709-4328-BDA1-0A73B51D0E82'
)
BEGIN
    INSERT INTO dbo.StudyNoteMetrics (StudyNoteId, MetricDefinitionId, MetricValue)
    VALUES ('D1935CF8-F5AD-4A26-AD67-61FAD368BB12', 'B2B182D0-8709-4328-BDA1-0A73B51D0E82', 14);
END;
