/// <summary>
/// 스코어 서버 API 공통 베이스 URL (stage·user·leaderboard 공통 prefix).
/// 배포 프록시 경로가 /score 인 경우 아래처럼 유지합니다.
/// 로컬 Spring Boot 기본: http://localhost:8081/api
/// </summary>
public static class ScoreApiConfig
{
    public const string ApiBaseUrl = "http://158.179.161.203:8080/score/api";
    // public const string ApiBaseUrl = "http://localhost:8081/api";
}
