// src/services/personaservice.ts
import axios from '@/error-handling/axiosSetup';
import { Index } from '@/models/indexmetadata';

function getApiUrl(endpoint: string): string {
  const rootApiUrl = import.meta.env.VITE_AGILECHAT_API_URL as string;
  return `${rootApiUrl}/api/indexes/${endpoint}`;
}

export async function getIndexes(): Promise<Index[]> {
  const apiUrl = getApiUrl('');

  try {
    const response = await axios.get<Index[]>(apiUrl);
    return response.data;
  } catch (error) {
    console.error('Error fetching indexes:', error);
    return [];
  }
}

//Create new index in cosmos
export async function createIndex(newIndex: Partial<Index>): Promise<Index | null> {
  const apiUrl = getApiUrl('create');
  try {
    const indexData = {
      name: newIndex.name,
      description: newIndex.description,
      group: newIndex.group,
      createdBy: newIndex.createdBy,
    };
    const response = await axios.post<Index>(apiUrl, indexData, {
      headers: {
        'Content-Type': 'application/json',
      },
    });

    return response.data;
  } catch (error) {
    console.error('Error creating index:', error);
    return null;
  }
}

//Delete index in cosmos
export async function deleteIndex(id: string): Promise<boolean> {
  const apiUrl = getApiUrl(`delete/${id}`);
  try {
    await axios.delete(apiUrl);
    return true;
  } catch (error) {
    console.error(`Error deleting index with ID ${id}:`, error);
    return false;
  }
}
