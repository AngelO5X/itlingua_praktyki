"use client";

import { useMemo, useRef, useState } from "react";
import {
  ArrowDownToLine,
  ArrowUpFromLine,
  BookOpen,
  CalendarDays,
  GraduationCap,
  LayoutDashboard,
  LogOut,
  Settings,
  Users,
  Wallet,
} from "lucide-react";

import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { cn } from "@/lib/utils";

type StudentStatus = "Aktywny" | "Wstrzymane" | "Urlop" | "Zakończone";

type Payment = {
  id: string;
  date: string;
  amount: number;
  note: string;
};

type Student = {
  id: string;
  student: string;
  teacher: string;
  status: StudentStatus;
  balance: number;
  debt: number;
  payments: Payment[];
};

const STATUSES: StudentStatus[] = [
  "Aktywny",
  "Wstrzymane",
  "Urlop",
  "Zakończone",
];

const INITIAL_STUDENTS: Student[] = [
  {
    id: "1",
    student: "Magda Nowak",
    teacher: "Krystyna Czub",
    status: "Wstrzymane",
    balance: 320,
    debt: 60,
    payments: [
      { id: "p1", date: "12.08.2026", amount: 200, note: "Pakiet 8 lekcji" },
      { id: "p2", date: "03.07.2026", amount: 120, note: "Dopłata" },
    ],
  },
  {
    id: "2",
    student: "Jan Kowalski",
    teacher: "Anna Lewandowska",
    status: "Aktywny",
    balance: 480,
    debt: 0,
    payments: [
      { id: "p3", date: "28.08.2026", amount: 480, note: "Pakiet miesięczny" },
    ],
  },
  {
    id: "3",
    student: "Ewa Wiśniewska",
    teacher: "Krystyna Czub",
    status: "Aktywny",
    balance: 150,
    debt: 90,
    payments: [
      { id: "p4", date: "20.08.2026", amount: 150, note: "4 lekcje" },
    ],
  },
  {
    id: "4",
    student: "Piotr Zieliński",
    teacher: "Marek Nowicki",
    status: "Urlop",
    balance: 0,
    debt: 240,
    payments: [
      { id: "p5", date: "11.06.2026", amount: 80, note: "Częściowa wpłata" },
    ],
  },
  {
    id: "5",
    student: "Olga Kamińska",
    teacher: "Anna Lewandowska",
    status: "Zakończone",
    balance: 40,
    debt: 0,
    payments: [
      { id: "p6", date: "02.05.2026", amount: 400, note: "Rozliczenie kursu" },
    ],
  },
];

const NAV_ITEMS = [
  { id: "students", label: "Uczniowie", icon: Users },
  { id: "teachers", label: "Nauczyciele", icon: GraduationCap },
  { id: "schedule", label: "Grafik", icon: CalendarDays },
  { id: "courses", label: "Kursy", icon: BookOpen },
  { id: "payments", label: "Płatności", icon: Wallet },
  { id: "overview", label: "Pulpit", icon: LayoutDashboard },
  { id: "settings", label: "Ustawienia", icon: Settings },
] as const;

function formatPln(value: number) {
  return `${value}zł`;
}

function toCsv(students: Student[]) {
  const header = ["Uczeń", "Nauczyciel", "Status", "Saldo", "Dług"];
  const rows = students.map((s) => [
    s.student,
    s.teacher,
    s.status,
    String(s.balance),
    String(s.debt),
  ]);
  return [header, ...rows]
    .map((row) =>
      row.map((cell) => `"${cell.replaceAll('"', '""')}"`).join(";"),
    )
    .join("\n");
}

function parseCsv(text: string): Student[] {
  const lines = text
    .replace(/^\uFEFF/, "")
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean);

  const dataLines = lines.slice(1);
  return dataLines.map((line, index) => {
    const cols = line.split(";").map((col) => col.replaceAll(/^"|"$/g, "").trim());
    return {
      id: `import-${Date.now()}-${index}`,
      student: cols[0] || "Nieznany uczeń",
      teacher: cols[1] || "—",
      status: (STATUSES.includes(cols[2] as StudentStatus)
        ? cols[2]
        : "Aktywny") as StudentStatus,
      balance: Number(cols[3]) || 0,
      debt: Number(cols[4]) || 0,
      payments: [],
    };
  });
}

export default function AdminPanelPage() {
  const [students, setStudents] = useState<Student[]>(INITIAL_STUDENTS);
  const [selectedId, setSelectedId] = useState(INITIAL_STUDENTS[0].id);
  const [activeNav, setActiveNav] = useState<(typeof NAV_ITEMS)[number]["id"]>(
    "students",
  );
  const [sheetOpen, setSheetOpen] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const selected = useMemo(
    () => students.find((student) => student.id === selectedId) ?? null,
    [students, selectedId],
  );

  function selectStudent(id: string) {
    setSelectedId(id);
    if (typeof window !== "undefined" && window.matchMedia("(max-width: 1023px)").matches) {
      setSheetOpen(true);
    }
  }

  function updateSelected(patch: Partial<Student>) {
    if (!selected) return;
    setStudents((prev) =>
      prev.map((student) =>
        student.id === selected.id ? { ...student, ...patch } : student,
      ),
    );
  }

  function exportExcel() {
    const csv = `\uFEFF${toCsv(students)}`;
    const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = "uczniowie.csv";
    link.click();
    URL.revokeObjectURL(url);
  }

  function importExcel(file: File) {
    const reader = new FileReader();
    reader.onload = () => {
      const parsed = parseCsv(String(reader.result ?? ""));
      if (parsed.length === 0) return;
      setStudents(parsed);
      setSelectedId(parsed[0].id);
    };
    reader.readAsText(file);
  }

  const detailPanel = selected ? (
    <DetailPanel
      student={selected}
      onChange={updateSelected}
      onConfirm={() => setSheetOpen(false)}
    />
  ) : (
    <p className="text-sm text-zinc-500">Wybierz ucznia z tabeli.</p>
  );

  return (
    <div className="flex min-h-dvh flex-col bg-zinc-950 text-zinc-100">
      <header className="flex h-16 shrink-0 items-center justify-between border-b border-zinc-800 px-4 sm:px-6">
        <div className="flex items-center gap-3">
          <div className="flex size-8 items-center justify-center rounded-sm bg-red-600 text-sm font-semibold text-white">
            L
          </div>
          <Avatar className="size-9 ring-1 ring-red-600/80">
            <AvatarFallback className="bg-red-600 text-xs font-medium text-white">
              IN
            </AvatarFallback>
          </Avatar>
          <div className="leading-tight">
            <p className="text-sm font-medium text-white">Imię Nazwisko</p>
            <p className="text-xs text-zinc-500">admin/nauczyciel</p>
          </div>
        </div>
        <Button
          variant="ghost"
          className="h-8 px-2 text-xs font-medium tracking-widest text-zinc-300 hover:bg-transparent hover:text-white"
        >
          <LogOut className="size-3.5" />
          WYLOGUJ
        </Button>
      </header>

      <div className="flex min-h-0 flex-1">
        <aside className="flex w-16 shrink-0 flex-col items-center gap-3 border-r border-zinc-800 py-5">
          {NAV_ITEMS.map((item) => {
            const Icon = item.icon;
            const isActive = item.id === activeNav;
            return (
              <Tooltip key={item.id}>
                <TooltipTrigger
                  render={
                    <button
                      type="button"
                      aria-label={item.label}
                      aria-current={isActive ? "page" : undefined}
                      onClick={() => setActiveNav(item.id)}
                      className={cn(
                        "flex size-10 items-center justify-center rounded-md border text-zinc-400 transition-colors hover:text-white",
                        isActive
                          ? "border-red-600 bg-zinc-900 text-white"
                          : "border-transparent hover:border-zinc-700",
                      )}
                    />
                  }
                >
                  <Icon className="size-4" />
                </TooltipTrigger>
                <TooltipContent side="right">{item.label}</TooltipContent>
              </Tooltip>
            );
          })}
        </aside>

        <main className="flex min-w-0 flex-1 gap-0 p-3 sm:p-5">
          <section className="flex min-w-0 flex-1 flex-col rounded-2xl border border-zinc-800 bg-zinc-900/70">
            <div className="flex items-center justify-end gap-1 px-3 pt-3 sm:px-4">
              <input
                ref={fileInputRef}
                type="file"
                accept=".csv,.txt,application/vnd.ms-excel"
                className="hidden"
                onChange={(event) => {
                  const file = event.target.files?.[0];
                  if (file) importExcel(file);
                  event.target.value = "";
                }}
              />
              <Tooltip>
                <TooltipTrigger
                  render={
                    <Button
                      variant="ghost"
                      size="icon-sm"
                      aria-label="Import Excel"
                      className="text-zinc-400 hover:text-white"
                      onClick={() => fileInputRef.current?.click()}
                    />
                  }
                >
                  <ArrowDownToLine />
                </TooltipTrigger>
                <TooltipContent>Import Excel (CSV)</TooltipContent>
              </Tooltip>
              <Tooltip>
                <TooltipTrigger
                  render={
                    <Button
                      variant="ghost"
                      size="icon-sm"
                      aria-label="Eksport Excel"
                      className="text-zinc-400 hover:text-white"
                      onClick={exportExcel}
                    />
                  }
                >
                  <ArrowUpFromLine />
                </TooltipTrigger>
                <TooltipContent>Eksport Excel (CSV)</TooltipContent>
              </Tooltip>
            </div>

            <div className="min-h-0 flex-1 overflow-auto px-2 pb-4 sm:px-3">
              <Table>
                <TableHeader>
                  <TableRow className="border-zinc-700 hover:bg-transparent">
                    <TableHead className="bg-zinc-700/80 px-4 text-[11px] font-semibold tracking-wider text-zinc-200 uppercase">
                      Uczeń
                    </TableHead>
                    <TableHead className="bg-zinc-700/80 px-4 text-[11px] font-semibold tracking-wider text-zinc-200 uppercase">
                      Nauczyciel
                    </TableHead>
                    <TableHead className="bg-zinc-700/80 px-4 text-[11px] font-semibold tracking-wider text-zinc-200 uppercase">
                      Status
                    </TableHead>
                    <TableHead className="bg-zinc-700/80 px-4 text-right text-[11px] font-semibold tracking-wider text-zinc-200 uppercase">
                      Saldo
                    </TableHead>
                    <TableHead className="bg-zinc-700/80 px-4 text-right text-[11px] font-semibold tracking-wider text-zinc-200 uppercase">
                      Dług
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {students.map((student) => {
                    const isSelected = student.id === selectedId;
                    return (
                      <TableRow
                        key={student.id}
                        data-state={isSelected ? "selected" : undefined}
                        tabIndex={0}
                        aria-selected={isSelected}
                        onClick={() => selectStudent(student.id)}
                        onKeyDown={(event) => {
                          if (event.key === "Enter" || event.key === " ") {
                            event.preventDefault();
                            selectStudent(student.id);
                          }
                        }}
                        className={cn(
                          "cursor-pointer border-zinc-800 text-zinc-200 hover:bg-zinc-800/60 data-[state=selected]:bg-transparent",
                          isSelected &&
                            "relative z-10 rounded-md shadow-[inset_0_0_0_1px_#dc2626]",
                        )}
                      >
                        <TableCell className="px-4 py-3 font-medium text-white">
                          {student.student}
                        </TableCell>
                        <TableCell className="px-4 py-3 text-zinc-300">
                          {student.teacher}
                        </TableCell>
                        <TableCell className="px-4 py-3 text-zinc-300">
                          {student.status}
                        </TableCell>
                        <TableCell className="px-4 py-3 text-right font-medium text-emerald-500">
                          {formatPln(student.balance)}
                        </TableCell>
                        <TableCell className="px-4 py-3 text-right font-medium text-red-500">
                          {formatPln(student.debt)}
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </div>
          </section>

          <aside className="hidden w-[300px] shrink-0 flex-col border-l border-zinc-800 pl-5 lg:flex xl:w-[340px]">
            {detailPanel}
          </aside>
        </main>
      </div>

      <Sheet open={sheetOpen} onOpenChange={setSheetOpen}>
        <SheetContent
          side="right"
          className="w-full max-w-sm border-zinc-800 bg-zinc-950 p-5 lg:hidden"
        >
          <SheetHeader className="p-0">
            <SheetTitle>Edycja ucznia</SheetTitle>
            <SheetDescription>
              Zaktualizuj dane wybranego rekordu i zatwierdź zmiany.
            </SheetDescription>
          </SheetHeader>
          <div className="min-h-0 flex-1 overflow-y-auto pt-4">{detailPanel}</div>
        </SheetContent>
      </Sheet>
    </div>
  );
}

function DetailPanel({
  student,
  onChange,
  onConfirm,
}: {
  student: Student;
  onChange: (patch: Partial<Student>) => void;
  onConfirm: () => void;
}) {
  return (
    <form
      className="flex h-full min-h-0 flex-col"
      onSubmit={(event) => {
        event.preventDefault();
        onConfirm();
      }}
    >
      <div className="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto pr-1">
        <Field label="Uczeń" htmlFor="student-name">
          <Input
            id="student-name"
            value={student.student}
            onChange={(event) => onChange({ student: event.target.value })}
            className="h-9 border-zinc-700 bg-zinc-900 text-zinc-100"
          />
        </Field>
        <Field label="Nauczyciel" htmlFor="teacher-name">
          <Input
            id="teacher-name"
            value={student.teacher}
            onChange={(event) => onChange({ teacher: event.target.value })}
            className="h-9 border-zinc-700 bg-zinc-900 text-zinc-100"
          />
        </Field>
        <Field label="Status" htmlFor="student-status">
          <Select
            value={student.status}
            onValueChange={(value) => {
              if (value) onChange({ status: value as StudentStatus });
            }}
          >
            <SelectTrigger
              id="student-status"
              className="h-9 w-full border-zinc-700 bg-zinc-900 text-zinc-100"
            >
              <SelectValue />
            </SelectTrigger>
            <SelectContent className="border-zinc-700 bg-zinc-900">
              {STATUSES.map((status) => (
                <SelectItem key={status} value={status}>
                  {status}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>
        <Field label="Saldo" htmlFor="student-balance">
          <Input
            id="student-balance"
            type="number"
            min={0}
            value={student.balance}
            onChange={(event) =>
              onChange({ balance: Number(event.target.value) || 0 })
            }
            className="h-9 border-zinc-700 bg-zinc-900 text-zinc-100"
          />
        </Field>
        <Field label="Dług" htmlFor="student-debt">
          <Input
            id="student-debt"
            type="number"
            min={0}
            value={student.debt}
            onChange={(event) =>
              onChange({ debt: Number(event.target.value) || 0 })
            }
            className="h-9 border-zinc-700 bg-zinc-900 text-zinc-100"
          />
        </Field>

        <div className="space-y-2">
          <p className="text-sm text-zinc-400">Historia wpłat</p>
          <ul className="space-y-2">
            {student.payments.length === 0 ? (
              <li className="rounded-md bg-zinc-800/80 px-3 py-3 text-xs text-zinc-500">
                Brak wpłat
              </li>
            ) : (
              student.payments.map((payment) => (
                <li
                  key={payment.id}
                  className="flex items-center justify-between rounded-md bg-zinc-800/90 px-3 py-2.5"
                >
                  <div>
                    <p className="text-xs text-zinc-400">{payment.date}</p>
                    <p className="text-sm text-zinc-200">{payment.note}</p>
                  </div>
                  <span className="text-sm font-medium text-emerald-500">
                    +{formatPln(payment.amount)}
                  </span>
                </li>
              ))
            )}
          </ul>
        </div>
      </div>

      <div className="flex justify-end pt-5">
        <Button
          type="submit"
          variant="outline"
          className="h-9 border-red-600 bg-zinc-950 px-5 text-white hover:bg-red-600/10 hover:text-white"
        >
          Zatwierdź
        </Button>
      </div>
    </form>
  );
}

function Field({
  label,
  htmlFor,
  children,
}: {
  label: string;
  htmlFor: string;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-1.5">
      <Label htmlFor={htmlFor} className="text-sm font-normal text-zinc-400">
        {label}
      </Label>
      {children}
    </div>
  );
}
