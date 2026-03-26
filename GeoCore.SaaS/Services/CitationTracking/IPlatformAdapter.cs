using GeoCore.SaaS.Services.CitationTracking.Models;

namespace GeoCore.SaaS.Services.CitationTracking;

/// <summary>
/// AI 平台适配器接口
/// </summary>
public interface IPlatformAdapter
{
    /// <summary>
    /// 平台名称
    /// </summary>
    AIPlatform Platform { get; }
    
    /// <summary>
    /// 平台权重（用于计算综合指标）
    /// </summary>
    double Weight { get; }
    
    /// <summary>
    /// 是否可用
    /// </summary>
    bool IsAvailable { get; }
    
    /// <summary>
    /// 发送查询并获取响应
    /// </summary>
    Task<PlatformResponse> QueryAsync(string question, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 分析引用情况
    /// </summary>
    Task<CitationAnalysisResult> AnalyzeCitationAsync(
        PlatformResponse response, 
        string brandName, 
        List<string> brandAliases,
        List<string> competitors,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 平台适配器基类
/// </summary>
public abstract class PlatformAdapterBase : IPlatformAdapter
{
    protected readonly ILogger _logger;
    protected readonly ICitationAnalyzer _citationAnalyzer;
    
    public abstract AIPlatform Platform { get; }
    public abstract double Weight { get; }
    public virtual bool IsAvailable => true;
    
    protected PlatformAdapterBase(ILogger logger, ICitationAnalyzer citationAnalyzer)
    {
        _logger = logger;
        _citationAnalyzer = citationAnalyzer;
    }
    
    public abstract Task<PlatformResponse> QueryAsync(string question, CancellationToken cancellationToken = default);
    
    public virtual async Task<CitationAnalysisResult> AnalyzeCitationAsync(
        PlatformResponse response,
        string brandName,
        List<string> brandAliases,
        List<string> competitors,
        CancellationToken cancellationToken = default)
    {
        if (!response.Success || string.IsNullOrEmpty(response.Response))
        {
            return new CitationAnalysisResult { IsCited = false };
        }
        
        return await _citationAnalyzer.AnalyzeAsync(
            response.Response,
            brandName,
            brandAliases,
            competitors,
            response.DetectedLinks,
            cancellationToken);
    }
}

/// <summary>
/// 引用分析器接口
/// </summary>
public interface ICitationAnalyzer
{
    Task<CitationAnalysisResult> AnalyzeAsync(
        string response,
        string brandName,
        List<string> brandAliases,
        List<string> competitors,
        List<string> detectedLinks,
        CancellationToken cancellationToken = default);
}
