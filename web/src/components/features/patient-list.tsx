"use client";

import { useState, useMemo } from "react";
import Link from "next/link";
import { AgGridReact } from "ag-grid-react";
import type { ColDef, ICellRendererParams } from "ag-grid-community";
import "ag-grid-community/styles/ag-grid.css";
import "ag-grid-community/styles/ag-theme-alpine.css";
import { usePatients } from "@/hooks/queries/use-patients";
import { useDebounce } from "@/hooks/use-debounce";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import type { Patient } from "@/schemas/patient.schema";

function NameCellRenderer({ data }: ICellRendererParams<Patient>) {
  if (!data) return null;
  return (
    <Link
      href={`/patients/${data.id}`}
      className="text-primary hover:underline font-medium"
    >
      {data.fullName}
    </Link>
  );
}

function StatusCellRenderer({ value }: ICellRendererParams<Patient>) {
  const variantMap: Record<string, "success" | "secondary" | "destructive" | "default"> = {
    Active: "success",
    Inactive: "secondary",
    Deceased: "destructive",
    Transferred: "default",
  };
  return <Badge variant={variantMap[value] ?? "secondary"}>{value}</Badge>;
}

function DateCellRenderer({ value }: ICellRendererParams<Patient>) {
  if (!value) return null;
  return <span>{new Date(value).toLocaleDateString()}</span>;
}

export function PatientList() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<string>("");
  const debouncedSearch = useDebounce(search, 300);

  const { data, isLoading, isError, error } = usePatients({
    page,
    pageSize: 20,
    status: status || undefined,
    searchTerm: debouncedSearch || undefined,
  });

  const columnDefs = useMemo<ColDef<Patient>[]>(
    () => [
      {
        field: "fullName",
        headerName: "Name",
        cellRenderer: NameCellRenderer,
        flex: 2,
        minWidth: 180,
      },
      {
        field: "email",
        headerName: "Email",
        flex: 2,
        minWidth: 200,
      },
      {
        field: "status",
        headerName: "Status",
        cellRenderer: StatusCellRenderer,
        width: 130,
      },
      {
        headerName: "Location",
        valueGetter: (params) =>
          params.data ? `${params.data.city}, ${params.data.state}` : "",
        flex: 1,
        minWidth: 150,
      },
      {
        field: "updatedAt",
        headerName: "Updated",
        cellRenderer: DateCellRenderer,
        width: 120,
      },
    ],
    []
  );

  if (isError) {
    return (
      <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-4" role="alert">
        <p className="text-destructive">Failed to load patients: {error.message}</p>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="space-y-4">
        <div className="flex flex-col sm:flex-row gap-3">
          <Skeleton className="h-9 w-full sm:w-64" />
          <Skeleton className="h-9 w-full sm:w-40" />
        </div>
        <Skeleton className="h-[600px] w-full" />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-3 sm:items-center">
        <Input
          type="search"
          placeholder="Search patients..."
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
          className="w-full sm:w-64"
          aria-label="Search patients"
        />
        <Select
          value={status}
          onChange={(e) => {
            setStatus(e.target.value);
            setPage(1);
          }}
          className="w-full sm:w-40"
          aria-label="Filter by status"
        >
          <option value="">All Statuses</option>
          <option value="Active">Active</option>
          <option value="Inactive">Inactive</option>
          <option value="Deceased">Deceased</option>
          <option value="Transferred">Transferred</option>
        </Select>
        {data && (
          <span className="text-sm text-muted-foreground">
            {data.totalCount} patient{data.totalCount !== 1 ? "s" : ""}
          </span>
        )}
      </div>

      {/* AG Grid */}
      <div className="ag-theme-alpine rounded-lg border overflow-hidden" style={{ height: "min(600px, calc(100vh - 280px))" }}>
        <AgGridReact<Patient>
          rowData={data?.items ?? []}
          columnDefs={columnDefs}
          getRowId={(params) => params.data.id}
          domLayout="normal"
          suppressMovableColumns
          animateRows
          overlayNoRowsTemplate="<span class='text-muted-foreground'>No patients found.</span>"
        />
      </div>

      {/* Pagination */}
      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-between">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page === 1}
          >
            Previous
          </Button>
          <span className="text-sm text-muted-foreground">
            Page {data.page} of {data.totalPages}
          </span>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setPage((p) => Math.min(data.totalPages, p + 1))}
            disabled={page === data.totalPages}
          >
            Next
          </Button>
        </div>
      )}
    </div>
  );
}
