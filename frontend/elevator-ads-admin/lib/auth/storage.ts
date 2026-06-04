"use client";

const TOKEN_STORAGE_KEY = "elevator-ads-admin-token";
const ROLE_STORAGE_KEY = "elevator-ads-admin-role";

function canUseBrowserStorage() {
  return typeof window !== "undefined";
}

function setCookie(name: string, value: string) {
  document.cookie = `${name}=${encodeURIComponent(value)}; path=/; SameSite=Lax`;
}

function clearCookie(name: string) {
  document.cookie = `${name}=; path=/; Max-Age=0; SameSite=Lax`;
}

export function getToken(): string | null {
  if (!canUseBrowserStorage()) {
    return null;
  }

  return window.localStorage.getItem(TOKEN_STORAGE_KEY);
}

export function setToken(token: string): void {
  if (!canUseBrowserStorage()) {
    return;
  }

  window.localStorage.setItem(TOKEN_STORAGE_KEY, token);
  setCookie(TOKEN_STORAGE_KEY, token);
}

export function clearToken(): void {
  if (!canUseBrowserStorage()) {
    return;
  }

  window.localStorage.removeItem(TOKEN_STORAGE_KEY);
  clearCookie(TOKEN_STORAGE_KEY);
}

export function getRole(): string | null {
  if (!canUseBrowserStorage()) {
    return null;
  }

  return window.localStorage.getItem(ROLE_STORAGE_KEY);
}

export function setRole(role: string): void {
  if (!canUseBrowserStorage()) {
    return;
  }

  window.localStorage.setItem(ROLE_STORAGE_KEY, role);
}

export function clearRole(): void {
  if (!canUseBrowserStorage()) {
    return;
  }

  window.localStorage.removeItem(ROLE_STORAGE_KEY);
}
