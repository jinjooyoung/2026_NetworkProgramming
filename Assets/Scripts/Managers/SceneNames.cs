/// <summary>
/// 씬 이름을 한 곳에서 관리하는 정적 클래스
/// 기존 씬(MainLobbyScene, 0, 1, 2 ...)을 백업으로 남겨두고 "_New" 씬을 사용하기 위해 추가
///
/// [원래 씬으로 롤백하는 법]
/// 1. Suffix 값을 "" 로 변경
/// 2. File > Build Profiles(Build Settings)의 씬 목록을 원래 씬으로 교체
/// </summary>
public static class SceneNames
{
    public const string Suffix = "_New";

    public static string MainLobby => "MainLobbyScene" + Suffix;
    public static string GameSelect => "GameSelectScene" + Suffix;
    public static string Game(int gameIndex) => gameIndex + Suffix;
}
