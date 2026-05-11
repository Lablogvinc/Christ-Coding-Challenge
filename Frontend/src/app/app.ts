import {Component, OnInit} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import { ChangeDetectorRef } from '@angular/core';

interface AggregateData {
  Language: string;
  Mat?: string;
  Mat2?: string;
  Mat3?: string;
  Mrk?: string;
  Leg?: string;
  Leg2?: string;
  Leg3?: string;
  Ziel?: string;
  Wrg2?: string;
  Whg2?: string;
  Koll?: string;
  Count: number;
}

interface ArticleDetail {
  Id: string;
  ArticleId: string;
  Mat?: string;
  Mat2?: string;
  Mat3?: string;
  Mrk?: string;
  Leg?: string;
  Leg2?: string;
  Leg3?: string;
  Ziel?: string;
  Wrg2?: string;
  Whg2?: string;
  Koll?: string;
  Far?: string;
  Agr?: string;
  Stil?: string;
  WRG_HYB?: string;
  Opt?: string;
  Gol?: string;
  MITMAS_CFI4?: string;
}

@Component({
  selector: 'app-root',
  imports: [CommonModule, FormsModule],
  template: `
    <div style="position: relative; padding: 20px;">
      <select [(ngModel)]="selectedLanguage" (ngModelChange)="onLanguageChange()" style="position: absolute; top: 20px; right: 20px;">
        <option *ngFor="let lang of languages" [value]="lang">{{ lang }}</option>
      </select>
      <h1>Aggregierte Daten</h1>
      <table border="1" style="width: 100%; border-collapse: collapse;">
        <thead>
          <tr>
            <th>Sprache</th>
            <th>Material</th>
            <th>Material 2</th>
            <th>Material 3</th>
            <th>Marke</th>
            <th>Legierung</th>
            <th>Legierung 2</th>
            <th>Legierung 3</th>
            <th>Ziel-Geschlecht</th>
            <th>Warengruppe</th>
            <th>Warenhauptgruppe</th>
            <th>Kollektion</th>
            <th>Anzahl</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let item of filteredAggregates" (click)="selectItem(item)" [style.cursor]="'pointer'" [style.backgroundColor]="isSelected(item) ? '#e0e0e0' : 'transparent'">
            <td>{{ item.Language }}</td>
            <td>{{ item.Mat || 'k. A.' }}</td>
            <td>{{ item.Mat2 || 'k. A.' }}</td>
            <td>{{ item.Mat3 || 'k. A.' }}</td>
            <td>{{ item.Mrk || 'k. A.' }}</td>
            <td>{{ item.Leg || 'k. A.' }}</td>
            <td>{{ item.Leg2 || 'k. A.' }}</td>
            <td>{{ item.Leg3 || 'k. A.' }}</td>
            <td>{{ item.Ziel || 'k. A.' }}</td>
            <td>{{ item.Wrg2 || 'k. A.' }}</td>
            <td>{{ item.Whg2 || 'k. A.' }}</td>
            <td>{{ item.Koll || 'k. A.' }}</td>
            <td>{{ item.Count }}</td>
          </tr>
        </tbody>
      </table>
      <div *ngIf="selectedItem" style="margin-top: 30px; padding: 20px; border: 1px solid #ccc; border-radius: 5px;">
        <h2>Einzelne Artikel für ausgewählte Gruppe</h2>
        <table border="1" style="width: 100%; border-collapse: collapse;">
          <thead>
            <tr>
              <th>ID</th>
              <th>Artikel-ID</th>
              <th>Farbe</th>
              <th>Altersgruppe</th>
              <th>Stil</th>
              <th>Optik</th>
              <th>Warenhauptgruppe Hybris</th>
              <th>Warengruppe</th>
              <th>Warenhauptgruppe</th>
              <th>Go-Live Datum</th>
              <th>Kollektionsjahr</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let article of articles">
              <td>{{ article.Id }}</td>
              <td>{{ article.ArticleId }}</td>
              <td>{{ article.Far || 'k. A.' }}</td>
              <td>{{ article.Agr || 'k. A.' }}</td>
              <td>{{ article.Stil || 'k. A.' }}</td>
              <td>{{ article.Opt || 'k. A.' }}</td>
              <td>{{ article.WRG_HYB || 'k. A.' }}</td>
              <td>{{ article.Wrg2 || 'k. A.' }}</td>
              <td>{{ article.Whg2 || 'k. A.' }}</td>
              <td>{{ article.Gol || 'k. A.' }}</td>
              <td>{{ article.MITMAS_CFI4 || 'k. A.' }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  `,
  styleUrls: ['./app.css'],
})
export class App implements OnInit {
  title = 'Aggregated Data';
  aggregates: AggregateData[] = [];
  languages: string[] = ['da', 'de', 'fr', 'it', 'nl', 'pl', 'sv'];
  selectedLanguage = 'de';
  filteredAggregates: AggregateData[] = [];
  selectedItem: AggregateData | null = null;
  articles: ArticleDetail[] = [];

  constructor(private http: HttpClient, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.fetchAggregates();
  }

  fetchAggregates() {
    console.log('Fetching aggregates from http://localhost:5000/api/aggregates');
    this.http.get<AggregateData[]>('http://localhost:5000/api/aggregates').subscribe({
      next: (data) => {
        console.log('Aggregates received:', data);
        this.aggregates = data;
        const dataLanguages = [...new Set(data.map(item => item.Language))];
        this.languages = [...new Set([...this.languages, ...dataLanguages])].sort();
        this.onLanguageChange();
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error fetching aggregates:', error);
      }
    });
  }

  onLanguageChange() {
    this.filteredAggregates = this.aggregates.filter(item => item.Language === this.selectedLanguage);
    console.log(`Filtered aggregates for language "${this.selectedLanguage}":`, this.filteredAggregates);
    this.selectedItem = null;
    this.articles = [];
  }

  selectItem(item: AggregateData) {
    this.selectedItem = item;
    const params = new URLSearchParams({
      language: item.Language,
      mat: item.Mat || '',
      mat2: item.Mat2 || '',
      mat3: item.Mat3 || '',
      mrk: item.Mrk || '',
      leg: item.Leg || '',
      leg2: item.Leg2 || '',
      leg3: item.Leg3 || '',
      ziel: item.Ziel || '',
      wrg_2: item.Wrg2 || '',
      whg_2: item.Whg2 || '',
      koll: item.Koll || ''
    });

    console.log('Fetching articles with params:', params.toString());
    this.http.get<ArticleDetail[]>(`http://localhost:5000/api/articles?${params}`).subscribe({
      next: (data) => {
        console.log('Articles received:', data);
        this.articles = data;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error fetching articles:', error);
      }
    });
  }

  isSelected(item: AggregateData): boolean {
    return this.selectedItem === item;
  }
  
}
