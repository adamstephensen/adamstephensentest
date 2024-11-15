import { getIndexes } from '@/services/indexes-service';
import { Index } from '@/models/indexmetadata';
import { useEffect, useState } from 'react';

export function useIndexes() {
  const [indexes, setIndexes] = useState<Index[] | undefined>(undefined);
  const [indexesLoading, setIndexesLoading] = useState<boolean>(indexes === undefined);

  useEffect(() => {
    setIndexesLoading(true);

    getIndexes()
      .then(setIndexes)
      .finally(() => setIndexesLoading(false));
  }, []);

  return { indexes, indexesLoading };
}
