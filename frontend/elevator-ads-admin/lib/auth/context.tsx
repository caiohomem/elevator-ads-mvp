"use client";

import { createContext, useContext, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { clearRole, clearToken, getRole, getToken, setRole, setToken } from "@/lib/auth/storage";

interface AuthContextValue {
  token: string | null;
  role: string | null;
  isAuthenticated: boolean;
  isHydrated: boolean;
  login: (token: string, role: string) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const [token, setTokenState] = useState<string | null>(null);
  const [role, setRoleState] = useState<string | null>(null);
  const [isHydrated, setIsHydrated] = useState(false);

  useEffect(() => {
    let active = true;

    queueMicrotask(() => {
      if (!active) {
        return;
      }

      setTokenState(getToken());
      setRoleState(getRole());
      setIsHydrated(true);
    });

    return () => {
      active = false;
    };
  }, []);

  const login = (nextToken: string, nextRole: string) => {
    setToken(nextToken);
    setRole(nextRole);
    setTokenState(nextToken);
    setRoleState(nextRole);
  };

  const logout = () => {
    clearToken();
    clearRole();
    setTokenState(null);
    setRoleState(null);
    router.push("/login");
  };

  return (
    <AuthContext.Provider
      value={{
        token,
        role,
        isAuthenticated: Boolean(token),
        isHydrated,
        login,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error("useAuth must be used within AuthProvider");
  }

  return context;
}
