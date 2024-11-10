import axios from "axios";
import { Assistant } from "../types/Assistant";

function getApiUrl(endpoint: string): string {
  const rootApiUrl = import.meta.env.VITE_AGILECHAT_API_URL as string;
  return `${rootApiUrl}/${endpoint}`;
}

// Fetch all assistants
export async function fetchAssistants(): Promise<Assistant[] | null> {
  const apiUrl = getApiUrl("assistants");
  try {
    const response = await axios.get<Assistant[]>(apiUrl);
    return response.data;
  } catch (error) {
    console.error("Error fetching assistants:", error);
    return null;
  }
}

// Fetch a assistant by ID
export async function fetchAssistantById(
  id: string
): Promise<Assistant | null> {
  const apiUrl = getApiUrl(`assistants/${id}`);
  try {
    const response = await axios.get<Assistant>(apiUrl);
    return response.data;
  } catch (error) {
    console.error(`Error fetching assistant with ID ${id}:`, error);
    return null;
  }
}

// Create a new assistant
export async function createAssistant(
  newAssistant: Assistant
): Promise<Assistant | null> {
  const apiUrl = getApiUrl("assistants");
  try {
    // Ensure all required fields are included in the request
    const assistantData = {
      id: newAssistant.id,
      name: newAssistant.name,
      description: newAssistant.description,
      type: newAssistant.type,
      greeting: newAssistant.greeting,
      systemMessage: newAssistant.systemMessage,
      group: newAssistant.group,
      folder: newAssistant.folders,
      temperature: newAssistant.temperature,
      topP: newAssistant.topP,
      documentLimit: newAssistant.documentLimit,
      tools: newAssistant.tools, // Add the tools field here
      status: newAssistant.status,
      createdAt: new Date().toISOString(),
      createdBy: "adam@stephensen.me",
      updatedAt: new Date().toISOString(),
      updatedBy: "adam@stephensen.me",
    };

    const response = await axios.post<Assistant>(apiUrl, assistantData);
    return response.data;
  } catch (error) {
    console.error("Error creating assistant:", error);
    return null;
  }
}

export async function updateAssistant(
  updatedAssistant: Assistant
): Promise<Assistant | null> {
  const apiUrl = getApiUrl(`assistants/${updatedAssistant.id}`);
  console.log("apiUrl", apiUrl);
  try {
    // Ensure all required fields are included in the request
    const assistantData = {
      id: updatedAssistant.id,
      name: updatedAssistant.name,
      type: updatedAssistant.type,
      description: updatedAssistant.description,
      greeting: updatedAssistant.greeting,
      systemMessage: updatedAssistant.systemMessage,
      group: updatedAssistant.group,
      folder: updatedAssistant.folders,
      temperature: updatedAssistant.temperature,
      topP: updatedAssistant.topP,
      documentLimit: updatedAssistant.documentLimit,
      status: updatedAssistant.status,
      tools: updatedAssistant.tools, // Add the tools field here
      createdAt: updatedAssistant.createdAt,
      createdBy: updatedAssistant.createdBy,
      updatedAt: new Date().toISOString(),
      updatedBy: "adam@stephensen.me",

    };
    const response = await axios.put<Assistant>(apiUrl, assistantData, {
      headers: {
        "Content-Type": "application/json",
      },
    });

    if (response.status == 204) {
      return assistantData;
    } else {
      return response.data;
    }
  } catch (error) {
    console.error(
      `Error updating assistant with ID ${updatedAssistant.id}:`,
      error
    );
    return null;
  }
}

// Delete a assistant
export async function deleteAssistant(id: string): Promise<boolean> {
  const apiUrl = getApiUrl(`assistants/${id}`);
  try {
    await axios.delete(apiUrl);
    return true;
  } catch (error) {
    console.error(`Error deleting assistant with ID ${id}:`, error);
    return false;
  }
}