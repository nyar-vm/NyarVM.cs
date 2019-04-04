namespace Nyar.Dialect.Web.Nodes;

/// <summary>
///     ˮ�ϲ���
/// </summary>
public enum WebHydrationStrategy
{
    /// <summary>
    ///     ����ˮ��
    /// </summary>
    eager,

    /// <summary>
    ///     �ӳ�ˮ�ϣ��״ν���ʱ��
    /// </summary>
    lazy,

    /// <summary>
    ///     ����ʱˮ��
    /// </summary>
    idle,

    /// <summary>
    ///     ��ˮ�ϣ�����̬��
    /// </summary>
    none
}