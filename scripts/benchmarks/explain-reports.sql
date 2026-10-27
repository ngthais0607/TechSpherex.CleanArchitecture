-- The benchmark captures the exact parameterized EF SQL for count + page,
-- then executes EXPLAIN (ANALYZE, BUFFERS) with the same Npgsql parameters.
-- Run from repository root; Docker must be running:
-- dotnet test tests/Api.IntegrationTests --filter FullyQualifiedName~ReportPerformanceTests
-- Raw timings, SQL, parameters and plans: TestResults/performance-evidence/
-- SQL is captured by ReportPerformanceTests.QueryCapture, not a hand-maintained approximation.
DO $$ BEGIN
  IF current_database() <> 'snp_api_test' OR current_setting('snp.allow_report_benchmark', true) IS DISTINCT FROM 'true' THEN
    RAISE EXCEPTION 'Explain requires an explicitly enabled disposable benchmark DB';
  END IF;
END $$;
-- Prefix any captured statement with the following; bind its recorded parameters:
-- EXPLAIN (ANALYZE, BUFFERS) SELECT ...;
