using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoCore.SaaS.Services.AuthorityScore;

/// <summary>
/// 域名权威度评分服务
/// 基于预设域名表计算引用来源的权威度分数
/// </summary>
public class AuthorityScoreService
{
    private static readonly Dictionary<string, int> DomainAuthority = new(StringComparer.OrdinalIgnoreCase)
    {
        // 政府/教育 - 最高权威
        { ".gov", 95 },
        { ".edu", 92 },
        { ".gov.cn", 95 },
        { ".edu.cn", 92 },
        { ".ac.cn", 90 },
        { ".org", 75 },
        
        // 顶级科技公司
        { "google.com", 90 },
        { "microsoft.com", 90 },
        { "apple.com", 88 },
        { "amazon.com", 88 },
        { "github.com", 88 },
        { "stackoverflow.com", 85 },
        { "developer.mozilla.org", 88 },
        { "w3.org", 90 },
        
        // 知名百科/知识库
        { "wikipedia.org", 85 },
        { "wikimedia.org", 82 },
        { "britannica.com", 85 },
        
        // 国际权威媒体
        { "nytimes.com", 88 },
        { "bbc.com", 88 },
        { "bbc.co.uk", 88 },
        { "reuters.com", 88 },
        { "wsj.com", 87 },
        { "theguardian.com", 85 },
        { "forbes.com", 82 },
        { "bloomberg.com", 85 },
        { "economist.com", 85 },
        { "cnn.com", 80 },
        
        // 科技媒体
        { "techcrunch.com", 82 },
        { "wired.com", 80 },
        { "arstechnica.com", 78 },
        { "theverge.com", 78 },
        { "engadget.com", 75 },
        { "zdnet.com", 75 },
        { "venturebeat.com", 75 },
        
        // 中文权威媒体
        { "xinhuanet.com", 90 },
        { "people.com.cn", 88 },
        { "cctv.com", 88 },
        { "chinadaily.com.cn", 85 },
        { "china.com.cn", 85 },
        
        // 中文科技/知识平台
        { "csdn.net", 75 },
        { "zhihu.com", 72 },
        { "jianshu.com", 65 },
        { "juejin.cn", 70 },
        { "cnblogs.com", 68 },
        { "oschina.net", 68 },
        { "infoq.cn", 75 },
        { "36kr.com", 72 },
        
        // 中文门户
        { "sina.com.cn", 75 },
        { "sohu.com", 72 },
        { "163.com", 72 },
        { "qq.com", 75 },
        { "baidu.com", 78 },
        { "tencent.com", 80 },
        { "alibaba.com", 80 },
        
        // 学术/研究
        { "arxiv.org", 88 },
        { "nature.com", 92 },
        { "science.org", 92 },
        { "ieee.org", 88 },
        { "acm.org", 88 },
        { "springer.com", 85 },
        { "sciencedirect.com", 85 },
        { "researchgate.net", 78 },
        
        // 社交/UGC 平台（权威度较低但有价值）
        { "reddit.com", 65 },
        { "quora.com", 68 },
        { "medium.com", 70 },
        { "linkedin.com", 75 },
        { "twitter.com", 65 },
        { "x.com", 65 },
        { "youtube.com", 72 },
        
        // AI/ML 相关
        { "openai.com", 88 },
        { "anthropic.com", 85 },
        { "huggingface.co", 82 },
        { "deepmind.com", 88 },
        { "ai.google", 88 },
        
        // 云服务商
        { "aws.amazon.com", 88 },
        { "cloud.google.com", 88 },
        { "azure.microsoft.com", 88 },
        { "docs.aws.amazon.com", 85 },
        { "learn.microsoft.com", 85 }
    };

    /// <summary>
    /// 计算域名的权威度分数
    /// </summary>
    /// <param name="domain">域名（如 cloud.google.com）</param>
    /// <returns>权威度分数 0-100</returns>
    public int CalculateAuthorityScore(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return 50;

        domain = domain.ToLowerInvariant().Trim();
        
        // 移除 www. 前缀
        if (domain.StartsWith("www."))
            domain = domain[4..];

        // 1. 精确匹配
        if (DomainAuthority.TryGetValue(domain, out var exactScore))
            return exactScore;

        // 2. 后缀匹配（.gov, .edu 等）
        foreach (var suffix in new[] { ".gov.cn", ".edu.cn", ".ac.cn", ".gov", ".edu", ".org" })
        {
            if (domain.EndsWith(suffix) && DomainAuthority.TryGetValue(suffix, out var suffixScore))
                return suffixScore;
        }

        // 3. 子域名匹配（cloud.google.com -> google.com）
        var parts = domain.Split('.');
        if (parts.Length > 2)
        {
            // 尝试根域名
            var rootDomain = string.Join(".", parts.TakeLast(2));
            if (DomainAuthority.TryGetValue(rootDomain, out var rootScore))
                return rootScore;
            
            // 尝试二级域名（如 docs.aws.amazon.com -> aws.amazon.com）
            if (parts.Length > 3)
            {
                var secondLevel = string.Join(".", parts.TakeLast(3));
                if (DomainAuthority.TryGetValue(secondLevel, out var secondScore))
                    return secondScore;
            }
        }

        // 4. 默认值
        return 50;
    }

    /// <summary>
    /// 从 URL 提取域名并计算权威度
    /// </summary>
    /// <param name="url">完整 URL</param>
    /// <returns>权威度分数 0-100</returns>
    public int CalculateAuthorityScoreFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return 50;

        try
        {
            var uri = new Uri(url);
            return CalculateAuthorityScore(uri.Host);
        }
        catch
        {
            return 50;
        }
    }

    /// <summary>
    /// 批量计算多个来源的平均权威度
    /// </summary>
    /// <param name="domains">域名列表</param>
    /// <returns>平均权威度分数</returns>
    public int CalculateAverageAuthorityScore(IEnumerable<string> domains)
    {
        var domainList = domains?.ToList();
        if (domainList == null || domainList.Count == 0)
            return 50;

        var totalScore = domainList.Sum(d => CalculateAuthorityScore(d));
        return totalScore / domainList.Count;
    }

    /// <summary>
    /// 计算 TrustScore（信任度分数）
    /// TrustScore = (AuthorityScore + BrandFitIndex) / 2
    /// </summary>
    /// <param name="authorityScore">权威度分数</param>
    /// <param name="brandFitIndex">品牌契合度</param>
    /// <returns>信任度分数 0-100</returns>
    public static int CalculateTrustScore(int authorityScore, int brandFitIndex)
    {
        return (authorityScore + brandFitIndex) / 2;
    }

    /// <summary>
    /// 计算引用质量分数
    /// 综合考虑：引用数量、多样性、权威性
    /// </summary>
    /// <param name="sources">引用来源列表（域名）</param>
    /// <returns>引用质量分数 0-100</returns>
    public int CalculateCitationQuality(IEnumerable<string> sources)
    {
        var sourceList = sources?.ToList();
        if (sourceList == null || sourceList.Count == 0)
            return 0;

        // 1. 引用数量得分（每个引用 20 分，最高 100 分）
        var countScore = Math.Min(100, sourceList.Count * 20);

        // 2. 引用多样性（不同域名数 / 总引用数）
        var uniqueDomains = sourceList.Select(NormalizeDomain).Distinct().Count();
        var diversityScore = Math.Min(100, (int)(uniqueDomains * 100.0 / sourceList.Count * 1.5));

        // 3. 引用权威性（平均权威度）
        var authorityScore = CalculateAverageAuthorityScore(sourceList);

        // 综合计算：数量 30% + 多样性 30% + 权威性 40%
        return (int)(countScore * 0.3 + diversityScore * 0.3 + authorityScore * 0.4);
    }

    /// <summary>
    /// 标准化域名（提取根域名）
    /// </summary>
    private static string NormalizeDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return domain;

        domain = domain.ToLowerInvariant().Trim();
        if (domain.StartsWith("www."))
            domain = domain[4..];

        var parts = domain.Split('.');
        if (parts.Length > 2)
        {
            // 返回根域名
            return string.Join(".", parts.TakeLast(2));
        }

        return domain;
    }
}
