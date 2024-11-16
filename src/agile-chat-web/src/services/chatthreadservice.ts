import axios from 'axios';
  import { ChatThread, NewChatThread, Message, UpdateChatThreadTitle, ExtensionUpdate } from '@/types/ChatThread';

function getApiUrl(endpoint: string): string {
    const rootApiUrl = import.meta.env.VITE_AGILECHAT_API_URL as string;
    return `${rootApiUrl}/chat-threads${endpoint}`;
}

function isAxiosError(error: any): error is Error & {
    response?: {
        status: number;
        data: any;
    };
    config?: {
        url?: string;
    };
} {
    return error && error.isAxiosError === true;
}

 

// Fetch chat threads by user ID
export async function fetchChatThreads(userId: string): Promise<ChatThread[] | null> {
    const apiUrl = getApiUrl(`/user/${userId}`);
    try {
        const response = await axios.get<ChatThread[]>(apiUrl);
        return response.data.map(thread => ({
            ...thread,
            createdAt: new Date(thread.createdAt),
            lastMessageAt: new Date(thread.lastMessageAt)
        }));
    } catch (error) {
                return null;
    }
}

// Fetch chat messages by threadid
export async function fetchChatsbythreadid(threadId: string): Promise<Message[] | null> {
    const apiUrl = getApiUrl(`/threads/${threadId}`);
    try {
        const response = await axios.get<Message[]>(apiUrl);
        return response.data.map(thread => ({ ...thread }));
    } catch (error) {
         
        return null;
    }
}

export async function fetchChatThread(id: string): Promise<ChatThread | null> {
    const apiUrl = getApiUrl(`/${id}`);
    try {
        const response = await axios.get<ChatThread>(apiUrl);
        return {
            ...response.data,
            createdAt: new Date(response.data.createdAt),
            lastMessageAt: new Date(response.data.lastMessageAt)
        };
    } catch (error) {
        
        return null;
    }
}
 

export async function createChatThread(data?: NewChatThread): Promise<ChatThread | null> {
    const apiUrl = getApiUrl('');
    
    // Get assistantId from query string if it exists
    const urlParams = new URLSearchParams(window.location.search);
    const assistantId = urlParams.get('assistantId');
    
    try {
        // Add assistantId to data if it exists in query string
        const threadData = assistantId
            ? { ...data, assistantId }
            : data;
            
        const response = await axios.post<ChatThread>(apiUrl, threadData);
        return { ...response.data };
    } catch (error) {
       
        return null;
    }
}

export async function updateChatThread(chatThread: ChatThread): Promise<ChatThread | null> {
    const apiUrl = getApiUrl(`/${chatThread.id}`);
    try {
        const response = await axios.put<ChatThread>(apiUrl, chatThread);
        return {
            ...response.data,
            createdAt: new Date(response.data.createdAt),
            lastMessageAt: new Date(response.data.lastMessageAt)
        };
    } catch (error) {
         
        return null;
    }
}

export async function updateChatTitle({ id, title }: UpdateChatThreadTitle): Promise<ChatThread | null> {
    const apiUrl = getApiUrl(`/${id}/title`);
    try {
        const response = await axios.patch<ChatThread>(apiUrl, { title: title.substring(0, 30) });
        return {
            ...response.data,
            createdAt: new Date(response.data.createdAt),
            lastMessageAt: new Date(response.data.lastMessageAt)
        };
    } catch (error) {
         
        return null;
    }
}

export async function deleteChatThread(id: string, userid: string): Promise<boolean> {
    const apiUrl = getApiUrl(`/${id}`);
    try {
        await axios.delete(apiUrl, {
            params: { userid },
            headers: { 'Content-Type': 'application/json' }
        });
        return true;
    } catch (error) {
        
        return false;
    }
}

export async function addExtensionToChatThread(data: ExtensionUpdate): Promise<ChatThread | null> {
    const apiUrl = getApiUrl(`/${data.chatThreadId}/extensions`);
    try {
        const response = await axios.post<ChatThread>(apiUrl, { extensionId: data.extensionId });
        return {
            ...response.data,
            createdAt: new Date(response.data.createdAt),
            lastMessageAt: new Date(response.data.lastMessageAt)
        };
    } catch (error) {
        
        return null;
    }
}

export async function removeExtensionFromChatThread(data: ExtensionUpdate): Promise<ChatThread | null> {
    const apiUrl = getApiUrl(`/${data.chatThreadId}/extensions/${data.extensionId}`);
    try {
        const response = await axios.delete<ChatThread>(apiUrl);
        return {
            ...response.data,
            createdAt: new Date(response.data.createdAt),
            lastMessageAt: new Date(response.data.lastMessageAt)
        };
    } catch (error) {
         
        return null;
    }
}

export async function createChatAndRedirect(): Promise<void> {
    try {
        const newThread = await createChatThread({
            name: "",
            userId: "",
            personaMessage: "",
            personaMessageTitle: ""
        });
        if (newThread) {
            window.location.href = `/chat/${newThread.id}`;
        }
    } catch (error) {
         
    }
}

 

export type { ChatThread, NewChatThread, UpdateChatThreadTitle, ExtensionUpdate };