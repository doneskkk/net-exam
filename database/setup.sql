CREATE DATABASE IF NOT EXISTS event_management_demo;
USE event_management_demo;

CREATE TABLE IF NOT EXISTS Events (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Title VARCHAR(150) NOT NULL,
    EventType VARCHAR(80) NOT NULL,
    Location VARCHAR(120) NOT NULL,
    EventDate DATETIME NOT NULL,
    Capacity INT NOT NULL,
    AvailableSeats INT NOT NULL,
    CONSTRAINT CHK_Events_Capacity CHECK (Capacity > 0),
    CONSTRAINT CHK_Events_AvailableSeats CHECK (AvailableSeats >= 0 AND AvailableSeats <= Capacity)
);

CREATE TABLE IF NOT EXISTS Participants (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    FullName VARCHAR(120) NOT NULL,
    Email VARCHAR(150) NOT NULL UNIQUE,
    Phone VARCHAR(40) NOT NULL
);

CREATE TABLE IF NOT EXISTS Registrations (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    EventId INT NOT NULL,
    ParticipantId INT NOT NULL,
    Status ENUM('Pending', 'Confirmed', 'Cancelled') NOT NULL DEFAULT 'Pending',
    RegisteredAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_Registrations_Events
        FOREIGN KEY (EventId) REFERENCES Events(Id)
        ON UPDATE CASCADE
        ON DELETE CASCADE,
    CONSTRAINT FK_Registrations_Participants
        FOREIGN KEY (ParticipantId) REFERENCES Participants(Id)
        ON UPDATE CASCADE
        ON DELETE CASCADE,
    CONSTRAINT UQ_Registrations_EventParticipant UNIQUE (EventId, ParticipantId)
);

DROP VIEW IF EXISTS vw_event_registrations;

CREATE VIEW vw_event_registrations AS
SELECT
    r.Id,
    r.EventId,
    r.ParticipantId,
    e.Title AS EventTitle,
    e.EventType,
    e.Location,
    p.FullName AS ParticipantName,
    p.Email AS ParticipantEmail,
    r.Status,
    r.RegisteredAt
FROM Registrations r
INNER JOIN Events e ON e.Id = r.EventId
INNER JOIN Participants p ON p.Id = r.ParticipantId;

DROP PROCEDURE IF EXISTS sp_add_participant;
DROP PROCEDURE IF EXISTS sp_register_participant;
DROP PROCEDURE IF EXISTS sp_update_registration_status;

DELIMITER $$

CREATE PROCEDURE sp_add_participant(
    IN p_full_name VARCHAR(120),
    IN p_email VARCHAR(150),
    IN p_phone VARCHAR(40)
)
BEGIN
    INSERT INTO Participants (FullName, Email, Phone)
    VALUES (p_full_name, p_email, p_phone);
END $$

CREATE PROCEDURE sp_register_participant(
    IN p_event_id INT,
    IN p_participant_id INT,
    IN p_status VARCHAR(20)
)
BEGIN
    DECLARE v_available_seats INT;
    DECLARE v_exists INT;

    START TRANSACTION;

    SELECT COUNT(*)
    INTO v_exists
    FROM Registrations
    WHERE EventId = p_event_id AND ParticipantId = p_participant_id;

    IF v_exists > 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Participant is already registered for this event.';
    END IF;

    SELECT AvailableSeats
    INTO v_available_seats
    FROM Events
    WHERE Id = p_event_id
    FOR UPDATE;

    IF v_available_seats IS NULL THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Selected event does not exist.';
    END IF;

    IF v_available_seats <= 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'No available seats for this event.';
    END IF;

    INSERT INTO Registrations (EventId, ParticipantId, Status)
    VALUES (p_event_id, p_participant_id, p_status);

    UPDATE Events
    SET AvailableSeats = AvailableSeats - 1
    WHERE Id = p_event_id;

    COMMIT;
END $$

CREATE PROCEDURE sp_update_registration_status(
    IN p_registration_id INT,
    IN p_new_status VARCHAR(20)
)
BEGIN
    DECLARE v_old_status VARCHAR(20);
    DECLARE v_event_id INT;
    DECLARE v_available_seats INT;

    START TRANSACTION;

    SELECT EventId, Status
    INTO v_event_id, v_old_status
    FROM Registrations
    WHERE Id = p_registration_id
    FOR UPDATE;

    IF v_event_id IS NULL THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Registration not found.';
    END IF;

    SELECT AvailableSeats
    INTO v_available_seats
    FROM Events
    WHERE Id = v_event_id
    FOR UPDATE;

    IF v_old_status <> 'Cancelled' AND p_new_status = 'Cancelled' THEN
        UPDATE Events
        SET AvailableSeats = AvailableSeats + 1
        WHERE Id = v_event_id;
    ELSEIF v_old_status = 'Cancelled' AND p_new_status <> 'Cancelled' THEN
        IF v_available_seats <= 0 THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'No available seats to restore this registration.';
        END IF;

        UPDATE Events
        SET AvailableSeats = AvailableSeats - 1
        WHERE Id = v_event_id;
    END IF;

    UPDATE Registrations
    SET Status = p_new_status
    WHERE Id = p_registration_id;

    COMMIT;
END $$

DELIMITER ;

INSERT INTO Events (Title, EventType, Location, EventDate, Capacity, AvailableSeats)
SELECT 'Tech Conference 2026', 'Conference', 'Chisinau Arena', '2026-04-15 10:00:00', 120, 120
WHERE NOT EXISTS (SELECT 1 FROM Events WHERE Title = 'Tech Conference 2026');

INSERT INTO Events (Title, EventType, Location, EventDate, Capacity, AvailableSeats)
SELECT 'Marketing Workshop', 'Workshop', 'Business Hub', '2026-04-21 14:00:00', 40, 40
WHERE NOT EXISTS (SELECT 1 FROM Events WHERE Title = 'Marketing Workshop');

INSERT INTO Events (Title, EventType, Location, EventDate, Capacity, AvailableSeats)
SELECT 'Startup Meetup', 'Meetup', 'Digital Park', '2026-05-02 18:30:00', 80, 80
WHERE NOT EXISTS (SELECT 1 FROM Events WHERE Title = 'Startup Meetup');

INSERT INTO Participants (FullName, Email, Phone)
SELECT 'Ana Popescu', 'ana.popescu@example.com', '+37360000001'
WHERE NOT EXISTS (SELECT 1 FROM Participants WHERE Email = 'ana.popescu@example.com');

INSERT INTO Participants (FullName, Email, Phone)
SELECT 'Victor Ionescu', 'victor.ionescu@example.com', '+37360000002'
WHERE NOT EXISTS (SELECT 1 FROM Participants WHERE Email = 'victor.ionescu@example.com');
