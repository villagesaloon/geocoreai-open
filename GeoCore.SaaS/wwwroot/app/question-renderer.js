function renderGroupedQuestions(questions, models) {
    const container = document.getElementById('scenarioList');
    const grouped = {};
    questions.forEach(q => {
        const key = q.question;
        if (!grouped[key]) grouped[key] = { 
            question: q.question, 
            persona: q.persona, 
            stage: q.stage, 
            keyword: q.keyword, 
            models: [],
            // v4.0: 问题来源
            source: q.source || 'ai',
            sourceDetail: q.sourceDetail || '',
            sourceUrl: q.sourceUrl || ''
        };
        grouped[key].models.push({ name: q.model?.name || 'GPT', color: q.model?.color, freqScore: q.freqScore, answer: q.answer, sources: q.sources });
    });
    
    const colorMap = { green: 'bg-green-100 text-green-700', orange: 'bg-orange-100 text-orange-700', blue: 'bg-blue-100 text-blue-700', purple: 'bg-purple-100 text-purple-700', cyan: 'bg-cyan-100 text-cyan-700', rose: 'bg-rose-100 text-rose-700' };
    
    container.innerHTML = Object.values(grouped).map((g, i) => {
        const modelRows = g.models.map(m => {
            const mc = colorMap[m.color] || 'bg-gray-100 text-gray-600';
            return '<tr class="border-t"><td class="px-2 py-1"><span class="text-xs ' + mc + ' px-2 py-0.5 rounded">' + m.name + '</span></td><td class="px-2 py-1 text-xs">' + (m.freqScore ? '<span class="text-amber-600">' + m.freqScore + '</span>' : '-') + '</td><td class="px-2 py-1 text-xs text-gray-600 max-w-md truncate">' + (m.answer || '-') + '</td><td class="px-2 py-1 text-xs text-blue-500">' + (m.sources?.join(', ') || '-') + '</td></tr>';
        }).join('');
        // v4.0: 显示问题来源标识
        const sourceTag = g.source === 'real' 
            ? '<span class="text-xs bg-cyan-100 text-cyan-700 px-2 py-0.5 rounded"><i class="fas fa-globe mr-1"></i>' + (g.sourceDetail || '真实问题') + '</span>'
            : '<span class="text-xs bg-indigo-100 text-indigo-600 px-2 py-0.5 rounded"><i class="fas fa-robot mr-1"></i>AI生成</span>';
        return '<div class="mb-3 border rounded-lg overflow-hidden"><div class="p-3 bg-indigo-50 cursor-pointer flex items-center justify-between" onclick="this.nextElementSibling.classList.toggle(\'hidden\')"><p class="text-sm font-medium text-gray-800">' + (i+1) + '. "' + g.question + '"</p><div class="flex gap-2">' + sourceTag + '<span class="text-xs bg-blue-50 text-blue-600 px-2 py-0.5 rounded">' + (g.persona?.emoji || '') + ' ' + (g.persona?.name || '') + '</span><span class="text-xs bg-emerald-50 text-emerald-600 px-2 py-0.5 rounded">' + (g.stage?.emoji || '') + ' ' + (g.stage?.name || '') + '</span><span class="text-xs bg-gray-100 text-gray-500 px-2 py-0.5 rounded">' + g.models.length + ' 模型</span></div></div><div class="hidden"><table class="w-full text-left"><thead class="bg-gray-50"><tr><th class="px-2 py-1 text-xs text-gray-500">模型</th><th class="px-2 py-1 text-xs text-gray-500">热度</th><th class="px-2 py-1 text-xs text-gray-500">答案</th><th class="px-2 py-1 text-xs text-gray-500">来源</th></tr></thead><tbody>' + modelRows + '</tbody></table></div></div>';
    }).join('');
}
