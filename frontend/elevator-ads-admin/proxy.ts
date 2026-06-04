import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

const TOKEN_COOKIE = "elevator-ads-admin-token";

export function proxy(request: NextRequest) {
  const tokenFromCookie = request.cookies.get(TOKEN_COOKIE)?.value;
  const authorization = request.headers.get("authorization");
  const hasBearerToken = authorization?.startsWith("Bearer ");

  if (tokenFromCookie || hasBearerToken) {
    return NextResponse.next();
  }

  const loginUrl = new URL("/login", request.url);
  loginUrl.searchParams.set("next", request.nextUrl.pathname);
  return NextResponse.redirect(loginUrl);
}

export const config = {
  matcher: ["/((?!api|login|forbidden|_next|favicon.ico|.*\\..*).*)"],
};
