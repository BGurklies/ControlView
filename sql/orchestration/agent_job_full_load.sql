-- Job:      ControlView_Orchestration_FullLoad_Daily
-- Zeitplan: Daily_0200, taeglich 02:00 Uhr
-- Schritte:
--   1. T-SQL — Execute Full Load
--
-- Vor dem Ausfuehren:
--   @DataPath auf das CSV-Verzeichnis von generate_data.py setzen.

USE msdb;
GO

DECLARE @DataPath NVARCHAR(500) = N'D:\Code\VCS Projects\ControlView\data\raw';

-- Job anlegen, falls nicht vorhanden
IF NOT EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = N'ControlView_Orchestration_FullLoad_Daily')
    EXEC dbo.sp_add_job
        @job_name    = N'ControlView_Orchestration_FullLoad_Daily';

-- Zeitplan anlegen, falls nicht vorhanden: taeglich 02:00 Uhr
IF NOT EXISTS (SELECT 1 FROM msdb.dbo.sysschedules WHERE name = N'Daily_0200')
    EXEC dbo.sp_add_schedule
        @schedule_name     = N'Daily_0200',
        @freq_type         = 4,
        @freq_interval     = 1,
        @active_start_time = 020000;

DECLARE @job_id UNIQUEIDENTIFIER;
SELECT @job_id = job_id FROM msdb.dbo.sysjobs WHERE name = N'ControlView_Orchestration_FullLoad_Daily';

-- Vorhandene Schritte entfernen, damit sie in der richtigen Reihenfolge neu entstehen.
-- sp_delete_jobstep nummeriert die verbleibenden Schritte herunter; wiederholtes
-- Loeschen von step_id = 1 leert die Liste vollstaendig.
WHILE EXISTS (SELECT 1 FROM msdb.dbo.sysjobsteps WHERE job_id = @job_id)
    EXEC dbo.sp_delete_jobstep @job_id = @job_id, @step_id = 1;

-- Schritt 1: vollstaendiger Ladelauf
-- sp_run_full_load laedt die vier Raw-Tabellen per BULK INSERT aus @DataPath und
-- baut daraus die sechs Mart-Dimensionen und fact_journal neu auf, transaktionssicher.
DECLARE @full_load_cmd NVARCHAR(MAX) =
    N'EXEC orchestration.sp_run_full_load @DataPath = ''' + @DataPath + N''';';

EXEC dbo.sp_add_jobstep
    @job_name          = N'ControlView_Orchestration_FullLoad_Daily',
    @step_name         = N'Execute Full Load',
    @step_id           = 1,
    @subsystem         = N'TSQL',
    @database_name     = N'ControlView',
    @command           = @full_load_cmd,
    @on_success_action = 1,
    @on_fail_action    = 2;

-- Zeitplan anhaengen, falls noch nicht verknuepft
IF NOT EXISTS (
    SELECT 1
    FROM msdb.dbo.sysjobschedules js
    JOIN msdb.dbo.sysjobs         j ON js.job_id      = j.job_id
    JOIN msdb.dbo.sysschedules    s ON js.schedule_id = s.schedule_id
    WHERE j.name = N'ControlView_Orchestration_FullLoad_Daily'
      AND s.name = N'Daily_0200'
)
    EXEC dbo.sp_attach_schedule
        @job_name      = N'ControlView_Orchestration_FullLoad_Daily',
        @schedule_name = N'Daily_0200';

-- Job auf diesem Server registrieren, falls noch nicht geschehen
IF NOT EXISTS (
    SELECT 1
    FROM msdb.dbo.sysjobservers js
    JOIN msdb.dbo.sysjobs       j ON js.job_id = j.job_id
    WHERE j.name = N'ControlView_Orchestration_FullLoad_Daily'
)
    EXEC dbo.sp_add_jobserver
        @job_name = N'ControlView_Orchestration_FullLoad_Daily';
GO
