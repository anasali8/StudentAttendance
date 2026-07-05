-- =======================================================
-- Dummy Data Insertion Script for Student Attendance DB
-- =======================================================

-- IMPORTANT: Run this script against your local database to populate it with testing data.

-- 1. Insert Users
SET IDENTITY_INSERT [Users] ON;
INSERT INTO [Users] ([Id], [Username], [PasswordHash], [Role], [AssociatedId], [IsActive]) VALUES
(1, 'admin', 'AQAAAAEAACcQAAAA...dummyhash...', 'Admin', '', 1),
(2, 'dr.smith', 'AQAAAAEAACcQAAAA...dummyhash...', 'Teacher', 'T-101', 1),
(3, 'dr.jones', 'AQAAAAEAACcQAAAA...dummyhash...', 'Teacher', 'T-102', 1);
SET IDENTITY_INSERT [Users] OFF;

-- 2. Insert Students
SET IDENTITY_INSERT [Students] ON;
INSERT INTO [Students] ([Id], [ExternalId], [FullName], [Email], [Department], [EnrollmentYear]) VALUES
(1, 'STU-2023-0001', 'Alice Johnson', 'alice@university.edu', 'Computer Science', 2023),
(2, 'STU-2023-0002', 'Bob Smith', 'bob@university.edu', 'Computer Science', 2023),
(3, 'STU-2024-0003', 'Charlie Brown', 'charlie@university.edu', 'Information Systems', 2024),
(4, 'STU-2024-0004', 'Diana Prince', 'diana@university.edu', 'Information Systems', 2024);
SET IDENTITY_INSERT [Students] OFF;

-- 3. Insert Fingerprints
-- Note: Fingerprints uses StudentId as its Primary Key. No IDENTITY_INSERT needed.
INSERT INTO [Fingerprints] ([StudentId], [EncryptedTemplate], [EnrollmentDate]) VALUES
(1, 0x0102030405, '2023-09-01T10:00:00'),
(2, 0x0102030405, '2023-09-01T10:05:00'),
(3, 0x0102030405, '2024-09-01T10:00:00');
-- Diana Prince (Student 4) does NOT have a fingerprint yet, for testing "Missing" status.

-- 4. Insert Courses
SET IDENTITY_INSERT [Courses] ON;
INSERT INTO [Courses] ([Id], [CourseCode], [CourseName], [TeacherId]) VALUES
(1, 'CS101', 'Introduction to Programming', 2),
(2, 'IS201', 'Database Management Systems', 3);
SET IDENTITY_INSERT [Courses] OFF;

-- 5. Insert CourseStudents (Many-to-Many Join Table)
-- No IDENTITY_INSERT needed.
INSERT INTO [CourseStudents] ([CoursesId], [StudentsId]) VALUES
(1, 1),
(1, 2),
(1, 3),
(2, 3),
(2, 4);

-- 6. Insert LectureSessions
SET IDENTITY_INSERT [LectureSessions] ON;
INSERT INTO [LectureSessions] ([Id], [CourseId], [ScheduledStart], [ScheduledEnd], [IsActive]) VALUES
(1, 1, '2026-07-05T09:00:00', '2026-07-05T10:30:00', 0), -- Past session
(2, 1, '2026-07-07T09:00:00', '2026-07-07T10:30:00', 1), -- Active/Upcoming session
(3, 2, '2026-07-06T13:00:00', '2026-07-06T14:30:00', 0); -- Past session
SET IDENTITY_INSERT [LectureSessions] OFF;

-- 7. Insert Attendances
-- Note: LatenessClassification is stored as a string enum based on EF Core configuration.
SET IDENTITY_INSERT [Attendances] ON;
INSERT INTO [Attendances] ([Id], [StudentId], [LectureSessionId], [Timestamp], [LatenessClassification], [IsManualOverride]) VALUES
(1, 1, 1, '2026-07-05T08:55:00', 'OnTime', 0),
(2, 2, 1, '2026-07-05T09:12:00', 'LateTier2', 0),
(3, 3, 1, NULL, 'Absent', 0),
(4, 3, 3, '2026-07-06T13:02:00', 'OnTime', 0),
(5, 4, 3, '2026-07-06T13:35:00', 'LateTier3', 1);
SET IDENTITY_INSERT [Attendances] OFF;

-- 8. Insert Predictions
SET IDENTITY_INSERT [Predictions] ON;
INSERT INTO [Predictions] ([Id], [StudentId], [CourseId], [NextLectureDate], [AbsenceProbability], [RiskFlag]) VALUES
(1, 1, 1, '2026-07-07T09:00:00', 0.05, 'Low'),
(2, 2, 1, '2026-07-07T09:00:00', 0.25, 'Medium'),
(3, 3, 1, '2026-07-07T09:00:00', 0.85, 'High');
SET IDENTITY_INSERT [Predictions] OFF;
