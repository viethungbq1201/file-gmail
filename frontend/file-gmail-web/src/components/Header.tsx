import { PaperPlaneTilt } from "@phosphor-icons/react";
import type { ReactNode } from "react";

interface HeaderProps {
  children?: ReactNode;
}

export function Header({ children }: HeaderProps) {
  return (
    <header className="app-header">
      <div className="mx-auto flex h-14 max-w-[560px] items-center justify-between gap-4 px-4">
        <div className="flex items-center gap-2.5">
          <span
            className="flex h-8 w-8 items-center justify-center rounded-lg bg-(--accent) text-(--on-accent)"
            aria-hidden="true"
          >
            <PaperPlaneTilt size={18} weight="bold" />
          </span>
          <span className="text-[15px] font-semibold tracking-tight text-(--text)">
            FileGmail
          </span>
        </div>
        {children}
      </div>
    </header>
  );
}