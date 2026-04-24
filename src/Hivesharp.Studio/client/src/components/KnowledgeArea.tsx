import { useState } from 'react';
import type { RagPipelineInfo, VectorSearchResult } from '@/types';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Separator } from '@/components/ui/separator';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { Search, Upload, ChevronDown, ChevronRight } from 'lucide-react';

interface KnowledgeAreaProps {
  pipeline: RagPipelineInfo | null;
  searchResults: VectorSearchResult[];
  searching: boolean;
  ingesting: boolean;
  onSearch: (query: string, topK: number) => void;
  onIngest: (content: string, source?: string, mimeType?: string) => void;
}

export default function KnowledgeArea({ pipeline, searchResults, searching, ingesting, onSearch, onIngest }: KnowledgeAreaProps) {
  const [query, setQuery] = useState('');
  const [topK, setTopK] = useState(5);
  const [ingestContent, setIngestContent] = useState('');
  const [ingestSource, setIngestSource] = useState('');
  const [ingestFormat, setIngestFormat] = useState<'text/plain' | 'text/markdown' | 'text/html'>('text/plain');
  const [expandedResults, setExpandedResults] = useState<Set<string>>(new Set());

  if (!pipeline) {
    return (
      <div className="flex-1 flex items-center justify-center text-muted-foreground">
        Select a knowledge base to get started
      </div>
    );
  }

  const handleSearch = () => {
    if (query.trim()) onSearch(query, topK);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSearch();
    }
  };

  const handleIngest = () => {
    if (ingestContent.trim()) {
      onIngest(ingestContent, ingestSource || undefined, ingestFormat);
      setIngestContent('');
      setIngestSource('');
    }
  };

  const toggleResult = (id: string) => {
    setExpandedResults((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const scoreColor = (score: number) => {
    if (score >= 0.8) return 'bg-green-500/20 text-green-400 border-green-500/30';
    if (score >= 0.5) return 'bg-yellow-500/20 text-yellow-400 border-yellow-500/30';
    return 'bg-red-500/20 text-red-400 border-red-500/30';
  };

  return (
    <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
      {/* Header */}
      <div className="shrink-0 px-6 py-4 border-b border-border flex items-center gap-3">
        <h2 className="font-semibold text-lg">{pipeline.indexName}</h2>
        <Badge variant="secondary">{pipeline.dimensions}d</Badge>
        <Badge variant="secondary">chunk {pipeline.chunkSize}</Badge>
        <Badge variant="secondary">overlap {pipeline.chunkOverlap}</Badge>
        <Badge variant="outline">backend: {pipeline.vectorStoreBackend}</Badge>
      </div>

      {/* Search section */}
      <div className="shrink-0 px-6 py-4 border-b border-border">
        <div className="text-xs font-medium text-muted-foreground uppercase tracking-wider mb-2">Search Preview</div>
        <div className="flex gap-2 items-center">
          <Input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Enter a query to search the knowledge base..."
            className="flex-1"
          />
          <select
            value={topK}
            onChange={(e) => setTopK(Number(e.target.value))}
            className="bg-muted border border-border rounded-md px-2 py-2 text-sm"
          >
            <option value={3}>Top 3</option>
            <option value={5}>Top 5</option>
            <option value={10}>Top 10</option>
          </select>
          <Button onClick={handleSearch} disabled={searching || !query.trim()}>
            <Search className="size-4 mr-1" />
            {searching ? 'Searching...' : 'Search'}
          </Button>
        </div>
      </div>

      {/* Results */}
      <ScrollArea className="flex-1 min-h-0">
        <div className="px-6 py-3 space-y-2">
          {searchResults.length > 0 && (
            <div className="text-xs text-muted-foreground mb-2">
              {searchResults.length} result{searchResults.length !== 1 ? 's' : ''} found
            </div>
          )}
          {searchResults.map((result, i) => (
            <Collapsible key={result.id} open={expandedResults.has(result.id)} onOpenChange={() => toggleResult(result.id)}>
              <div className="rounded-md border border-border bg-card">
                <CollapsibleTrigger className="w-full text-left px-4 py-3 flex items-start gap-3">
                  <span className="text-xs text-muted-foreground font-mono mt-0.5 shrink-0">#{i + 1}</span>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-1">
                      <Badge className={scoreColor(result.score)}>
                        {(result.score * 100).toFixed(1)}%
                      </Badge>
                      {result.metadata?.source != null && (
                        <span className="text-xs text-muted-foreground truncate">
                          {String(result.metadata.source)}
                        </span>
                      )}
                    </div>
                    <p className="text-sm text-foreground line-clamp-2">{result.text}</p>
                  </div>
                  {expandedResults.has(result.id) ? (
                    <ChevronDown className="size-4 text-muted-foreground shrink-0 mt-0.5" />
                  ) : (
                    <ChevronRight className="size-4 text-muted-foreground shrink-0 mt-0.5" />
                  )}
                </CollapsibleTrigger>
                <CollapsibleContent>
                  <Separator />
                  <div className="px-4 py-3">
                    <pre className="text-sm whitespace-pre-wrap font-mono bg-muted rounded-md p-3 overflow-x-auto">
                      {result.text}
                    </pre>
                    {Object.keys(result.metadata).length > 0 && (
                      <div className="mt-2">
                        <div className="text-xs font-medium text-muted-foreground mb-1">Metadata</div>
                        <pre className="text-xs font-mono bg-muted rounded-md p-2 overflow-x-auto">
                          {JSON.stringify(result.metadata, null, 2)}
                        </pre>
                      </div>
                    )}
                  </div>
                </CollapsibleContent>
              </div>
            </Collapsible>
          ))}
        </div>
      </ScrollArea>

      {/* Ingestion section */}
      <div className="shrink-0 border-t border-border px-6 py-4">
        <div className="text-xs font-medium text-muted-foreground uppercase tracking-wider mb-2">Ingest Document</div>
        <div className="flex gap-2 items-end">
          <div className="flex-1 space-y-2">
            <textarea
              value={ingestContent}
              onChange={(e) => setIngestContent(e.target.value)}
              placeholder="Paste document content here..."
              className="w-full bg-muted rounded-md px-3 py-2 text-sm resize-none border border-border focus:outline-none focus:ring-1 focus:ring-ring"
              rows={3}
            />
            <div className="flex gap-2">
              <Input
                value={ingestSource}
                onChange={(e) => setIngestSource(e.target.value)}
                placeholder="Source (optional, e.g. docs/intro.md)"
                className="flex-1"
              />
              <select
                value={ingestFormat}
                onChange={(e) => setIngestFormat(e.target.value as typeof ingestFormat)}
                className="bg-muted border border-border rounded-md px-2 py-2 text-sm"
              >
                <option value="text/plain">Text</option>
                <option value="text/markdown">Markdown</option>
                <option value="text/html">HTML</option>
              </select>
            </div>
          </div>
          <Button onClick={handleIngest} disabled={ingesting || !ingestContent.trim()} className="self-end">
            <Upload className="size-4 mr-1" />
            {ingesting ? 'Ingesting...' : 'Ingest'}
          </Button>
        </div>
      </div>
    </div>
  );
}
