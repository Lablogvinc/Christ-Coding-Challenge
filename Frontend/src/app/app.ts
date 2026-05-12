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
  IsClock: boolean;
  Bfa?: string;
  Bmat?: string;
  Bva?: string;
  Blg?: string;
  Gef?: string;
  Gdi?: string;
  Gdm?: string;
  Gsa?: string;
  Kal?: string;
  Sar?: string;
  Taf?: string;
  Wad?: string;
  Wka?: string;
  Zba?: string;
  Zbf?: string;
  Typu?: string;
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
        <div style="overflow-x: auto; max-height: 600px; overflow-y: auto;">
        <table border="1" style="width: 100%; border-collapse: collapse;">
          <thead>
            <tr>
              <th style="padding: 12px 8px;">ID</th>
              <th style="padding: 12px 8px;">Artikel-ID</th>
              <th style="padding: 12px 8px;">Farbe</th>
              <th style="padding: 12px 8px;">Altersgruppe</th>
              <th style="padding: 12px 8px;">Stil</th>
              <th style="padding: 12px 8px;">Optik</th>
              <th style="padding: 12px 8px;">Warenhauptgruppe Hybris</th>
              <th style="padding: 12px 8px;">Warengruppe</th>
              <th style="padding: 12px 8px;">Warenhauptgruppe</th>
              <th style="padding: 12px 8px;">Go-Live Datum</th>
              <th style="padding: 12px 8px;">Kollektionsjahr</th>
              <th *ngIf="articles.length > 0 && articles[0].IsClock" style="padding: 12px 8px;">Bandfarbe</th>
              <th *ngIf="articles.length > 0 && articles[0].IsClock" style="padding: 12px 8px;">Bandmaterial</th>
              <th *ngIf="articles.length > 0 && articles[0].IsClock" style="padding: 12px 8px;">Bandverlauf Anstoß</th>
              <th *ngIf="articles.length > 0 && articles[0].IsClock" style="padding: 12px 8px;">Bandlänge</th>
              <th *ngIf="articles.length > 0 && articles[0].IsClock" style="padding: 12px 8px;">Gehäuseform</th>
              <th *ngIf="articles.length > 0 && articles[0].IsClock" style="padding: 12px 8px;">Gehäuse Dicke</th>
              <th *ngIf="articles.length > 0 && articles[0].IsClock" style="padding: 12px 8px;">Gehäuse Durchmesser</th>
              <th *ngIf="articles.length > 0 && articles[0].IsClock" style="padding: 12px 8px;">Glasart</th>
              <th *ngIf="articles.length > 0 && articles[0].IsClock" style="padding: 12px 8px;">Kaliber</th>
              <th *ngIf="articles.length > 0 && articles[0].IsClock" style="padding: 12px 8px;">Schließenart</th>
              <th *ngIf="articles.length > 0 && articles[0].IsClock" style="padding: 12px 8px;">Technische Ausführung</th>
              <th *ngIf="articles.length > 0 && articles[0].IsClock" style="padding: 12px 8px;">Wasserdichte</th>
              <th *ngIf="articles.length > 0 && articles[0].IsClock" style="padding: 12px 8px;">Werkart</th>
              <th *ngIf="articles.length > 0 && articles[0].IsClock" style="padding: 12px 8px;">Zifferblatt Anzeige</th>
              <th *ngIf="articles.length > 0 && articles[0].IsClock" style="padding: 12px 8px;">Zifferblatt Farbe</th>
              <th *ngIf="articles.length > 0 && articles[0].IsClock" style="padding: 12px 8px;">Anwendungsbereich</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let article of articles">
              <td style="padding: 10px 8px;">{{ article.Id }}</td>
              <td style="padding: 10px 8px;">{{ article.ArticleId }}</td>
              <td style="padding: 10px 8px;">{{ article.Far || 'k. A.' }}</td>
              <td style="padding: 10px 8px;">{{ article.Agr || 'k. A.' }}</td>
              <td style="padding: 10px 8px;">{{ article.Stil || 'k. A.' }}</td>
              <td style="padding: 10px 8px;">{{ article.Opt || 'k. A.' }}</td>
              <td style="padding: 10px 8px;">{{ article.WRG_HYB || 'k. A.' }}</td>
              <td style="padding: 10px 8px;">{{ article.Wrg2 || 'k. A.' }}</td>
              <td style="padding: 10px 8px;">{{ article.Whg2 || 'k. A.' }}</td>
              <td style="padding: 10px 8px;">{{ article.Gol || 'k. A.' }}</td>
              <td style="padding: 10px 8px;">{{ article.MITMAS_CFI4 || 'k. A.' }}</td>
              <td *ngIf="article.IsClock" style="padding: 10px 8px;">{{ article.Bfa || 'k. A.' }}</td>
              <td *ngIf="article.IsClock" style="padding: 10px 8px;">{{ article.Bmat || 'k. A.' }}</td>
              <td *ngIf="article.IsClock" style="padding: 10px 8px;">{{ article.Bva || 'k. A.' }}</td>
              <td *ngIf="article.IsClock" style="padding: 10px 8px;">{{ article.Blg || 'k. A.' }}</td>
              <td *ngIf="article.IsClock" style="padding: 10px 8px;">{{ article.Gef || 'k. A.' }}</td>
              <td *ngIf="article.IsClock" style="padding: 10px 8px;">{{ article.Gdi || 'k. A.' }}</td>
              <td *ngIf="article.IsClock" style="padding: 10px 8px;">{{ article.Gdm || 'k. A.' }}</td>
              <td *ngIf="article.IsClock" style="padding: 10px 8px;">{{ article.Gsa || 'k. A.' }}</td>
              <td *ngIf="article.IsClock" style="padding: 10px 8px;">{{ article.Kal || 'k. A.' }}</td>
              <td *ngIf="article.IsClock" style="padding: 10px 8px;">{{ article.Sar || 'k. A.' }}</td>
              <td *ngIf="article.IsClock" style="padding: 10px 8px;">{{ article.Taf || 'k. A.' }}</td>
              <td *ngIf="article.IsClock" style="padding: 10px 8px;">{{ article.Wad || 'k. A.' }}</td>
              <td *ngIf="article.IsClock" style="padding: 10px 8px;">{{ article.Wka || 'k. A.' }}</td>
              <td *ngIf="article.IsClock" style="padding: 10px 8px;">{{ article.Zba || 'k. A.' }}</td>
              <td *ngIf="article.IsClock" style="padding: 10px 8px;">{{ article.Zbf || 'k. A.' }}</td>
              <td *ngIf="article.IsClock" style="padding: 10px 8px;">{{ article.Typu || 'k. A.' }}</td>
            </tr>
          </tbody>
        </table>
        </div>
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
    console.log('Fetching aggregates from /api/aggregates');
    this.http.get<AggregateData[]>('/api/aggregates').subscribe({
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
    this.http.get<ArticleDetail[]>(`/api/articles?${params}`).subscribe({
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
