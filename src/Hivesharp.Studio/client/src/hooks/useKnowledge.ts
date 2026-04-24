import { useState, useEffect, useCallback } from 'react';
import { fetchRagPipelines, queryIndex, ingestDocument } from '@/api';
import { toast } from '@/lib/toast';
import type { RagPipelineInfo, VectorSearchResult } from '@/types';

export function useKnowledge() {
  const [pipelines, setPipelines] = useState<RagPipelineInfo[]>([]);
  const [selectedPipeline, setSelectedPipeline] = useState<RagPipelineInfo | null>(null);
  const [searchResults, setSearchResults] = useState<VectorSearchResult[]>([]);
  const [searching, setSearching] = useState(false);
  const [ingesting, setIngesting] = useState(false);

  useEffect(() => {
    fetchRagPipelines().then(setPipelines).catch(() => {
      // RAG not configured — that's fine
    });
  }, []);

  const selectPipeline = useCallback((indexName: string | null) => {
    if (!indexName) {
      setSelectedPipeline(null);
      setSearchResults([]);
      return;
    }
    const pipeline = pipelines.find((p) => p.indexName === indexName) ?? null;
    setSelectedPipeline(pipeline);
    setSearchResults([]);
  }, [pipelines]);

  const search = useCallback(async (query: string, topK: number = 5) => {
    if (!selectedPipeline || !query.trim()) return;
    setSearching(true);
    try {
      const results = await queryIndex(selectedPipeline.indexName, query, topK);
      setSearchResults(results);
    } catch {
      toast.error('Search failed');
    } finally {
      setSearching(false);
    }
  }, [selectedPipeline]);

  const ingest = useCallback(async (content: string, source?: string, mimeType?: string) => {
    if (!selectedPipeline || !content.trim()) return;
    setIngesting(true);
    try {
      await ingestDocument(selectedPipeline.indexName, content, source, mimeType);
      toast.success('Document ingested successfully');
    } catch {
      toast.error('Ingestion failed');
    } finally {
      setIngesting(false);
    }
  }, [selectedPipeline]);

  return {
    pipelines,
    selectedPipeline,
    searchResults,
    searching,
    ingesting,
    selectPipeline,
    search,
    ingest,
  };
}
