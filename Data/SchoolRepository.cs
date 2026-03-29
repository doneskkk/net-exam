using ExamTest.Models;
using MySql.Data.MySqlClient;

namespace ExamTest.Data;

public sealed class SchoolRepository
{
    public async Task InitializeDatabaseAsync()
    {
        await using var serverConnection = AppDb.CreateServerConnection();
        await serverConnection.OpenAsync();

        const string createDatabaseSql = "CREATE DATABASE IF NOT EXISTS event_management_demo;";

        await using (var createDatabaseCommand = new MySqlCommand(createDatabaseSql, serverConnection))
        {
            await createDatabaseCommand.ExecuteNonQueryAsync();
        }

        await using var connection = AppDb.CreateConnection();
        await connection.OpenAsync();

        var setupCommands = new[]
        {
            """
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
            """,
            """
            CREATE TABLE IF NOT EXISTS Participants (
                Id INT PRIMARY KEY AUTO_INCREMENT,
                FullName VARCHAR(120) NOT NULL,
                Email VARCHAR(150) NOT NULL UNIQUE,
                Phone VARCHAR(40) NOT NULL
            );
            """,
            """
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
            """,
            "DROP VIEW IF EXISTS vw_event_registrations;",
            """
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
            """,
            "DROP PROCEDURE IF EXISTS sp_add_participant;",
            """
            CREATE PROCEDURE sp_add_participant(
                IN p_full_name VARCHAR(120),
                IN p_email VARCHAR(150),
                IN p_phone VARCHAR(40)
            )
            BEGIN
                INSERT INTO Participants (FullName, Email, Phone)
                VALUES (p_full_name, p_email, p_phone);
            END
            """,
            "DROP PROCEDURE IF EXISTS sp_register_participant;",
            """
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
            END
            """,
            "DROP PROCEDURE IF EXISTS sp_update_registration_status;",
            """
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
            END
            """
        };

        foreach (var sql in setupCommands)
        {
            await using var command = new MySqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        var seedCommands = new[]
        {
            """
            INSERT INTO Events (Title, EventType, Location, EventDate, Capacity, AvailableSeats)
            SELECT 'Tech Conference 2026', 'Conference', 'Chisinau Arena', '2026-04-15 10:00:00', 120, 120
            WHERE NOT EXISTS (SELECT 1 FROM Events WHERE Title = 'Tech Conference 2026');
            """,
            """
            INSERT INTO Events (Title, EventType, Location, EventDate, Capacity, AvailableSeats)
            SELECT 'Marketing Workshop', 'Workshop', 'Business Hub', '2026-04-21 14:00:00', 40, 40
            WHERE NOT EXISTS (SELECT 1 FROM Events WHERE Title = 'Marketing Workshop');
            """,
            """
            INSERT INTO Events (Title, EventType, Location, EventDate, Capacity, AvailableSeats)
            SELECT 'Startup Meetup', 'Meetup', 'Digital Park', '2026-05-02 18:30:00', 80, 80
            WHERE NOT EXISTS (SELECT 1 FROM Events WHERE Title = 'Startup Meetup');
            """,
            """
            INSERT INTO Participants (FullName, Email, Phone)
            SELECT 'Ana Popescu', 'ana.popescu@example.com', '+37360000001'
            WHERE NOT EXISTS (SELECT 1 FROM Participants WHERE Email = 'ana.popescu@example.com');
            """,
            """
            INSERT INTO Participants (FullName, Email, Phone)
            SELECT 'Victor Ionescu', 'victor.ionescu@example.com', '+37360000002'
            WHERE NOT EXISTS (SELECT 1 FROM Participants WHERE Email = 'victor.ionescu@example.com');
            """
        };

        foreach (var sql in seedCommands)
        {
            await using var command = new MySqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task<List<EventItem>> GetEventsAsync(string? typeFilter = null, string? locationFilter = null)
    {
        const string sql = """
            SELECT Id, Title, EventType, Location, EventDate, Capacity, AvailableSeats
            FROM Events
            WHERE (@TypeFilter IS NULL OR EventType = @TypeFilter)
              AND (@LocationFilter IS NULL OR Location LIKE CONCAT('%', @LocationFilter, '%'))
            ORDER BY EventDate, Title;
            """;

        var results = new List<EventItem>();

        await using var connection = AppDb.CreateConnection();
        await connection.OpenAsync();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TypeFilter", string.IsNullOrWhiteSpace(typeFilter) ? DBNull.Value : typeFilter);
        command.Parameters.AddWithValue("@LocationFilter", string.IsNullOrWhiteSpace(locationFilter) ? DBNull.Value : locationFilter);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new EventItem
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                EventType = reader.GetString(reader.GetOrdinal("EventType")),
                Location = reader.GetString(reader.GetOrdinal("Location")),
                EventDate = reader.GetDateTime(reader.GetOrdinal("EventDate")),
                Capacity = reader.GetInt32(reader.GetOrdinal("Capacity")),
                AvailableSeats = reader.GetInt32(reader.GetOrdinal("AvailableSeats"))
            });
        }

        return results;
    }

    public async Task AddEventAsync(EventItem eventItem)
    {
        const string sql = """
            INSERT INTO Events (Title, EventType, Location, EventDate, Capacity, AvailableSeats)
            VALUES (@Title, @EventType, @Location, @EventDate, @Capacity, @Capacity);
            """;

        await using var connection = AppDb.CreateConnection();
        await connection.OpenAsync();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Title", eventItem.Title);
        command.Parameters.AddWithValue("@EventType", eventItem.EventType);
        command.Parameters.AddWithValue("@Location", eventItem.Location);
        command.Parameters.AddWithValue("@EventDate", eventItem.EventDate);
        command.Parameters.AddWithValue("@Capacity", eventItem.Capacity);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateEventAsync(EventItem eventItem)
    {
        const string sql = """
            UPDATE Events
            SET Title = @Title,
                EventType = @EventType,
                Location = @Location,
                EventDate = @EventDate,
                Capacity = @Capacity,
                AvailableSeats = @AvailableSeats
            WHERE Id = @Id;
            """;

        const string activeRegistrationSql = """
            SELECT COUNT(*)
            FROM Registrations
            WHERE EventId = @EventId AND Status <> 'Cancelled';
            """;

        await using var connection = AppDb.CreateConnection();
        await connection.OpenAsync();

        await using (var countCommand = new MySqlCommand(activeRegistrationSql, connection))
        {
            countCommand.Parameters.AddWithValue("@EventId", eventItem.Id);
            var activeRegistrations = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
            if (eventItem.Capacity < activeRegistrations)
            {
                throw new InvalidOperationException("Capacity cannot be smaller than the number of active registrations.");
            }

            eventItem.AvailableSeats = eventItem.Capacity - activeRegistrations;
        }

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", eventItem.Id);
        command.Parameters.AddWithValue("@Title", eventItem.Title);
        command.Parameters.AddWithValue("@EventType", eventItem.EventType);
        command.Parameters.AddWithValue("@Location", eventItem.Location);
        command.Parameters.AddWithValue("@EventDate", eventItem.EventDate);
        command.Parameters.AddWithValue("@Capacity", eventItem.Capacity);
        command.Parameters.AddWithValue("@AvailableSeats", eventItem.AvailableSeats);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteEventAsync(int id)
    {
        const string sql = """
            DELETE FROM Events
            WHERE Id = @Id;
            """;

        await using var connection = AppDb.CreateConnection();
        await connection.OpenAsync();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<Participant>> GetParticipantsAsync(string? searchTerm = null)
    {
        const string sql = """
            SELECT Id, FullName, Email, Phone
            FROM Participants
            WHERE (@SearchTerm IS NULL
                OR FullName LIKE CONCAT('%', @SearchTerm, '%')
                OR Email LIKE CONCAT('%', @SearchTerm, '%'))
            ORDER BY FullName;
            """;

        var results = new List<Participant>();

        await using var connection = AppDb.CreateConnection();
        await connection.OpenAsync();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? DBNull.Value : searchTerm);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new Participant
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                Phone = reader.GetString(reader.GetOrdinal("Phone"))
            });
        }

        return results;
    }

    public async Task AddParticipantAsync(Participant participant)
    {
        await using var connection = AppDb.CreateConnection();
        await connection.OpenAsync();
        await using var command = new MySqlCommand("sp_add_participant", connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@p_full_name", participant.FullName);
        command.Parameters.AddWithValue("@p_email", participant.Email);
        command.Parameters.AddWithValue("@p_phone", participant.Phone);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateParticipantAsync(Participant participant)
    {
        const string sql = """
            UPDATE Participants
            SET FullName = @FullName,
                Email = @Email,
                Phone = @Phone
            WHERE Id = @Id;
            """;

        await using var connection = AppDb.CreateConnection();
        await connection.OpenAsync();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", participant.Id);
        command.Parameters.AddWithValue("@FullName", participant.FullName);
        command.Parameters.AddWithValue("@Email", participant.Email);
        command.Parameters.AddWithValue("@Phone", participant.Phone);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteParticipantAsync(int id)
    {
        const string sql = """
            DELETE FROM Participants
            WHERE Id = @Id;
            """;

        await using var connection = AppDb.CreateConnection();
        await connection.OpenAsync();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<RegistrationRecord>> GetRegistrationsAsync(string? searchTerm = null, string? statusFilter = null)
    {
        const string sql = """
            SELECT Id, EventId, ParticipantId, EventTitle, EventType, Location, ParticipantName, ParticipantEmail, Status, RegisteredAt
            FROM vw_event_registrations
            WHERE (@SearchTerm IS NULL
                OR ParticipantName LIKE CONCAT('%', @SearchTerm, '%')
                OR ParticipantEmail LIKE CONCAT('%', @SearchTerm, '%'))
              AND (@StatusFilter IS NULL OR Status = @StatusFilter)
            ORDER BY RegisteredAt DESC, Id DESC;
            """;

        var results = new List<RegistrationRecord>();

        await using var connection = AppDb.CreateConnection();
        await connection.OpenAsync();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? DBNull.Value : searchTerm);
        command.Parameters.AddWithValue("@StatusFilter", string.IsNullOrWhiteSpace(statusFilter) ? DBNull.Value : statusFilter);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new RegistrationRecord
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                EventId = reader.GetInt32(reader.GetOrdinal("EventId")),
                ParticipantId = reader.GetInt32(reader.GetOrdinal("ParticipantId")),
                EventTitle = reader.GetString(reader.GetOrdinal("EventTitle")),
                EventType = reader.GetString(reader.GetOrdinal("EventType")),
                Location = reader.GetString(reader.GetOrdinal("Location")),
                ParticipantName = reader.GetString(reader.GetOrdinal("ParticipantName")),
                ParticipantEmail = reader.GetString(reader.GetOrdinal("ParticipantEmail")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                RegisteredAt = reader.GetDateTime(reader.GetOrdinal("RegisteredAt"))
            });
        }

        return results;
    }

    public async Task CreateRegistrationAsync(int eventId, int participantId, string status)
    {
        await using var connection = AppDb.CreateConnection();
        await connection.OpenAsync();
        await using var command = new MySqlCommand("sp_register_participant", connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@p_event_id", eventId);
        command.Parameters.AddWithValue("@p_participant_id", participantId);
        command.Parameters.AddWithValue("@p_status", status);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateRegistrationStatusAsync(int registrationId, string status)
    {
        await using var connection = AppDb.CreateConnection();
        await connection.OpenAsync();
        await using var command = new MySqlCommand("sp_update_registration_status", connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@p_registration_id", registrationId);
        command.Parameters.AddWithValue("@p_new_status", status);
        await command.ExecuteNonQueryAsync();
    }
}
