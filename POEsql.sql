
-- EventEase Database Schema (Final Version for Submission)

-- EventTypes Table
CREATE TABLE EventTypes (
    EventTypeId INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL
);

-- Venues Table
CREATE TABLE Venues (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Location NVARCHAR(200) NOT NULL,
    Capacity INT NOT NULL,
    ImageURL NVARCHAR(500),
    IsAvailable BIT NOT NULL DEFAULT 1,
    EventTypeId INT NULL,
    FOREIGN KEY (EventTypeId) REFERENCES EventTypes(EventTypeId)
);

-- Events Table
CREATE TABLE Events (
    EventId INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX),
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    VenueId INT,
    FOREIGN KEY (VenueId) REFERENCES Venues(Id)
);

-- Bookings Table
CREATE TABLE Bookings (
    BookingId INT PRIMARY KEY IDENTITY(1,1),
    CustomerName NVARCHAR(100) NOT NULL,
    ContactEmail NVARCHAR(100) NOT NULL,
    BookingDate DATE NOT NULL,
    IsBooked BIT NOT NULL DEFAULT 1,
    VenueId INT NOT NULL,
    EventId INT NOT NULL,
    FOREIGN KEY (VenueId) REFERENCES Venues(Id),
    FOREIGN KEY (EventId) REFERENCES Events(EventId)
);
