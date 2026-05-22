import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Elevator Ads MVP",
  description: "Programmatic DOOH platform for elevator screens.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
